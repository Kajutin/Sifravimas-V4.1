using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.IO;

namespace Sifravimas_V4._1
{
    public partial class Form1 : Form
    {
        string path = "C:\\Users\\kajus\\Desktop\\testSaugumas\\";
        string globalName;
        string globalPassword;
        string globalLine;
        public Form1()
        {
            InitializeComponent();
            if (File.Exists(path + "passwords.txt.aes"))
            {
                FileDecrypt(path + "passwords.txt.aes", "test");
                File.Delete(path + "passwords.txt.aes");    
            }
            else if (File.Exists(path + "passwords.txt"))
            {
                //woops
            }
            else
            { 
                File.Create(path + "passwords.txt").Close();
            }
            FillListViewFromFile();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            FileEncrypt(path + "passwords.txt", "test");
            File.Delete(path + "passwords.txt");
            base.OnFormClosing(e);
        }
        private void FileEncrypt(string inputFile, string password)
        {
            //http://stackoverflow.com/questions/27645527/aes-encryption-on-large-files

            //generate random salt
            byte[] salt = GenerateRandomSalt();

            //create output file name
            using FileStream fsCrypt = new FileStream(inputFile + ".aes", FileMode.Create);
            //convert password string to byte arrray
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);

            //Set Rijndael symmetric encryption algorithm
            RijndaelManaged AES = new RijndaelManaged();
            AES.KeySize = 256;
            AES.BlockSize = 128;
            AES.Padding = PaddingMode.PKCS7;

            //http://stackoverflow.com/questions/2659214/why-do-i-need-to-use-the-rfc2898derivebytes-class-in-net-instead-of-directly
            //"What it does is repeatedly hash the user password along with the salt." High iteration counts.
            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);

            //Cipher modes: http://security.stackexchange.com/questions/52665/which-is-the-best-cipher-mode-and-padding-mode-for-aes-encryption
            AES.Mode = CipherMode.CBC;

            // write salt to the begining of the output file, so in this case can be random every time
            fsCrypt.Write(salt, 0, salt.Length);

            using CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateEncryptor(), CryptoStreamMode.Write);

            using FileStream fsIn = new FileStream(inputFile, FileMode.Open);

            //create a buffer (1mb) so only this amount will allocate in the memory and not the whole file
            byte[] buffer = new byte[1048576];
            int read;

            try
            {
                while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Application.DoEvents(); // -> for responsive GUI, using Task will be better!
                    cs.Write(buffer, 0, read);
                }
                // Close up
                fsIn.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            //finally
            //{
            //    cs.Close();
            //    fsCrypt.Close();
            //    using (var md5 = MD5.Create())
            //    {
            //        using (var stream = File.OpenRead(inputFile + ".aes"))
            //        {
            //            hashDictionary.Add(Convert.ToBase64String(md5.ComputeHash(stream)), inputFile + ".aes");
            //        }
            //    }
            //}
        }
        private void FileDecrypt(string inputFile, string password)
        {
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] salt = new byte[32];

            FileStream fsCrypt = new FileStream(inputFile, FileMode.Open);
            fsCrypt.Read(salt, 0, salt.Length);

            RijndaelManaged AES = new RijndaelManaged();
            AES.KeySize = 256;
            AES.BlockSize = 128;
            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);
            AES.Padding = PaddingMode.PKCS7;
            AES.Mode = CipherMode.CBC;
            string outputFile = inputFile.Substring(0, inputFile.Length - 4);

            CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateDecryptor(), CryptoStreamMode.Read);

            FileStream fsOut = new FileStream(outputFile, FileMode.Create);

            int read;
            byte[] buffer = new byte[1048576];

            try
            {
                while ((read = cs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Application.DoEvents();
                    fsOut.Write(buffer, 0, read);
                }
            }
            catch (CryptographicException ex_CryptographicException)
            {
                Console.WriteLine("CryptographicException error: " + ex_CryptographicException.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            try
            {
                cs.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error by closing CryptoStream: " + ex.Message);
            }
            finally
            {
                fsOut.Close();
                fsCrypt.Close();
            }
        }
        public static byte[] GenerateRandomSalt()
        {
            byte[] data = new byte[32];

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                for (int i = 0; i < 10; i++)
                {
                    // Fille the buffer with the generated data
                    rng.GetBytes(data);
                }
            }

            return data;
        }

        public void FillListViewFromFile()
        {
            PasswordsListView.Clear();
            if (new FileInfo(path + "passwords.txt").Length != 0)
            {
                int counter = 0;
                string line;

                // Read the file and display it line by line.  
                using System.IO.StreamReader file =
                    new System.IO.StreamReader(path + "passwords.txt");
                while ((line = file.ReadLine()) != null)
                {
                    string[] tempList = line.Split("<>");
                    string name = tempList[0];
                    string password = tempList[1];
                    string url = tempList[2];
                    string comment = tempList[3];
                    string passwordOutput;
                    byte[] KeyAES = Encoding.UTF8.GetBytes("testtesttesttest");
                    passwordOutput = DesifruojamBaitusITeksta_ECB(password, KeyAES);
                    PasswordsListView.Items.Add(name + " // " + passwordOutput + " // " + url + " // " + comment);
                    counter++;
                }
            }
            
        }
        private void SaveButton_Click(object sender, EventArgs e)
        {
            int found = 0;
            string Name = NameTextBox.Text;
            string Password = PasswordTextBox.Text;
            string URL = URLTextBox.Text;
            string Comment = CommentTextBox.Text;
            string PasswordEncrypted;
            if (new FileInfo(path + "passwords.txt").Length != 0)
            {
                int counter = 0;
                string line;

                // Read the file and display it line by line.  
                using System.IO.StreamReader file =
                    new System.IO.StreamReader(path + "passwords.txt");
                while ((line = file.ReadLine()) != null)
                {
                    string[] tempList = line.Split("<>");
                    string tempName = tempList[0];
                    if (Name == tempName)
                    {
                        MessageBox.Show("This name is taken, enter a new one.");
                        found++;
                    }    
                    counter++;
                }
            }
            if(found == 0)
            {
                byte[] KeyAES = Encoding.UTF8.GetBytes("testtesttesttest");
                string encrypted = SifruojamTekstaIBaitus_ECB(Password, KeyAES);
                string Final = Name + "<>" + encrypted + "<>" + URL + "<>" + Comment + "\n";
                File.AppendAllText(path + "passwords.txt", Final);
                FillListViewFromFile();
                NameTextBox.Text = "";
                PasswordTextBox.Text = "";
                URLTextBox.Text = "";
                CommentTextBox.Text = "";
            }

        }
        private string SifruojamTekstaIBaitus_ECB(string text, byte[] key)
        {
            // Nesifruotas tekstaspaverciamas i baitus
            byte[] tekstas = Encoding.UTF8.GetBytes(text);
            RijndaelManaged aes = new RijndaelManaged();
            // Modas ecb
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            aes.KeySize = 128;

            // Sukuria sifratoriu
            using (ICryptoTransform sifratorius = aes.CreateEncryptor(key, null))
            {
                // Sifruojam teksta
                byte[] sifruotasTekstas = sifratorius.TransformFinalBlock(tekstas, 0, tekstas.Length);
                // Nutraukiam darba
                sifratorius.Dispose();
                // Grazinam sifruota teksta string formatu
                return Convert.ToBase64String(sifruotasTekstas);
            }
        }
        private string DesifruojamBaitusITeksta_ECB(string text, byte[] key)
        {
            // Konvertuoja teksta i baitus
            byte[] sifruotasTekstas = Convert.FromBase64String(text);
            RijndaelManaged aes = new RijndaelManaged();
            // Nustatom rakto dydi
            aes.KeySize = 128;
            aes.Padding = PaddingMode.PKCS7;
            // Nuastatom moda i ecb
            aes.Mode = CipherMode.ECB;

            // Sukuria desifratoriu
            using (ICryptoTransform desifratorius = aes.CreateDecryptor(key, null))
            {
                byte[] desifruotasTekstas = desifratorius.TransformFinalBlock(sifruotasTekstas, 0, sifruotasTekstas.Length);
                // Nutraukia desifravimo darba
                desifratorius.Dispose();
                // Grazinam Desifruota teksta string formatu
                return Encoding.UTF8.GetString(desifruotasTekstas);
            }
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            string fullString = PasswordsListView.SelectedItems[0].Text;
            string[] stringList = fullString.Split(" // ");
            string passwordSnippet = stringList[1];
            Clipboard.SetText(passwordSnippet);
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            int counter = 0;
            string line;

            // Read the file and display it line by line.  
            using System.IO.StreamReader file =
                new System.IO.StreamReader(path + "passwords.txt");
            while ((line = file.ReadLine()) != null)
            {
                string[] tempList = line.Split("<>");
                globalName = tempList[0];
                globalPassword = tempList[1];
                globalLine = line;
                string url = tempList[2];
                string comment = tempList[3];
                if(SearchTextBox.Text == globalName)
                {
                    SearchResultLabel.Text = line;
                }
                counter++;
            }
        }
        private void ChangePasswordButton_Click(object sender, EventArgs e)
        {
            byte[] KeyAES = Encoding.UTF8.GetBytes("testtesttesttest");
            string newPasswordEncoded = SifruojamTekstaIBaitus_ECB(ChangePasswordTextBox.Text,KeyAES);
            File.WriteAllText(path + "passwords.txt", File.ReadAllText(path + "passwords.txt").Replace(globalPassword,newPasswordEncoded));
            FillListViewFromFile();
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            File.WriteAllText(path + "passwords.txt", File.ReadAllText(path + "passwords.txt").Replace(globalLine + "\n", ""));
            FillListViewFromFile();
            SearchResultLabel.Text = "Password was deleted.";
        }

        private void RevealPasswordButton_Click(object sender, EventArgs e)
        {
            byte[] KeyAES = Encoding.UTF8.GetBytes("testtesttesttest");
            RevealPasswordLabel.Text = DesifruojamBaitusITeksta_ECB(globalPassword, KeyAES);
        }

        private void RandomPasswordButton_Click(object sender, EventArgs e)
        {
            Guid g = Guid.NewGuid();
            string GuidString = Convert.ToBase64String(g.ToByteArray());
            GuidString = GuidString.Replace("=", "");
            GuidString = GuidString.Replace("+", "");
            GuidString = GuidString.Replace("/", "");
            PasswordTextBox.Text = GuidString;
        }
    }
}

