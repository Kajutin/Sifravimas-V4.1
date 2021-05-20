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
        #region Kintamieji
        string path = "C:\\Users\\kajus\\Desktop\\testSaugumas\\";
        string globalName;
        string globalPassword;
        string globalLine;
        bool shouldIRun = false;
        #endregion Kintamieji
        public Form1()
        {
            InitializeComponent();
            var login = new Login();
            //
            // PRISIJUNGIMO FORMA
            // jeigu teisingai suveda duomenis tai atsifruojamas failas
            //
            if(login.ShowDialog() == DialogResult.OK)
            {
                shouldIRun = true;
                

                //
                // SUKURIAMAS FAILAS ir ATSIFRUOJAMAS JEI YRA JAU
                //
                if (File.Exists(path + "passwords.txt.aes"))
                {
                    FileDecrypt(path + "passwords.txt.aes", "test");
                    File.Delete(path + "passwords.txt.aes");    
                }
                else if (File.Exists(path + "passwords.txt"))
                {
                    //woops, cia jeigu neteisingai programa buvo crashinta ar kas ir liko netycia neuzsifruotas failas
                }
                else
                { 
                    File.Create(path + "passwords.txt").Close();
                }
                FillListViewFromFile();
            }
        }

        //
        // UZSIFRAVIMAS UZDARANT PROGRAMA
        //
        protected override void OnFormClosing(FormClosingEventArgs e)
        { 
            if(shouldIRun == true)
            {
                FileEncrypt(path + "passwords.txt", "test");
                File.Delete(path + "passwords.txt");
                base.OnFormClosing(e);
            }    
        }
        
        //
        // FAILO SIFRAVIMO KODAS
        //
        private void FileEncrypt(string inputFile, string password)
        {
            byte[] salt = GenerateRandomSalt();
            using FileStream fsCrypt = new FileStream(inputFile + ".aes", FileMode.Create);
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            RijndaelManaged AES = new RijndaelManaged();
            AES.KeySize = 256;
            AES.BlockSize = 128;
            AES.Padding = PaddingMode.PKCS7;
            var key = new Rfc2898DeriveBytes(passwordBytes, salt, 50000);
            AES.Key = key.GetBytes(AES.KeySize / 8);
            AES.IV = key.GetBytes(AES.BlockSize / 8);
            AES.Mode = CipherMode.CBC;
            fsCrypt.Write(salt, 0, salt.Length);
            using CryptoStream cs = new CryptoStream(fsCrypt, AES.CreateEncryptor(), CryptoStreamMode.Write);
            using FileStream fsIn = new FileStream(inputFile, FileMode.Open);
            byte[] buffer = new byte[1048576];
            int read;
            try
            {
                while ((read = fsIn.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Application.DoEvents(); 
                    cs.Write(buffer, 0, read);
                }
                fsIn.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        //
        // FAILO ATSIFRAVIMO KODAS
        //
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

        //
        // DRUSKA KAD SKANIAU
        //
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

        //
        // SLAPTAZODZIU SARASO PILDYMAS IS FAILO
        //
        public void FillListViewFromFile()
        {
            PasswordsListView.Clear();
            if (new FileInfo(path + "passwords.txt").Length != 0)
            {
                int counter = 0;
                string line;


                using System.IO.StreamReader file =
                    new System.IO.StreamReader(path + "passwords.txt");
                while ((line = file.ReadLine()) != null)
                {
                    //
                    // EILUCIU NUSKAITYMAS
                    // eilute nuskaitoma ir isskaidoma i kelis punktus
                    //
                    string[] tempList = line.Split("<>");
                    string name = tempList[0];
                    string password = tempList[1];
                    string url = tempList[2];
                    string comment = tempList[3];
                    string passwordOutput;

                    //
                    // ATSIFRAVIMAS SLAPTAZODZIO
                    // kadangi is failo gauname AES uzsifruota slaptazodi tai ji rodant reikia atsifruoti
                    //
                    byte[] KeyAES = Encoding.UTF8.GetBytes("testtesttesttest");
                    passwordOutput = Decode(password, KeyAES);
                    PasswordsListView.Items.Add(name + " // " + passwordOutput + " // " + url + " // " + comment);
                    counter++;
                }
            }
            
        }

        //
        // NAUJO SLAPTAZODZIO KURIMAS
        //
        private void SaveButton_Click(object sender, EventArgs e)
        {
            int found = 0;
            string Name = NameTextBox.Text;
            string Password = PasswordTextBox.Text;
            string URL = URLTextBox.Text;
            string Comment = CommentTextBox.Text;
            string PasswordEncrypted;
            
            //
            // PATIKRA AR TOKS JAU VARDAS EGZISTUOJA
            //
            if (new FileInfo(path + "passwords.txt").Length != 0)
            {
                int counter = 0;
                string line;
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
                //
                // IVESTAS SLAPTAZODIS UZKODUOJAMAS ir IRASOMAS I FAILA
                //
                byte[] KeyAES = Encoding.UTF8.GetBytes("testtesttesttest");
                string encrypted = Encode(Password, KeyAES);
                string Final = Name + "<>" + encrypted + "<>" + URL + "<>" + Comment + "\n";
                File.AppendAllText(path + "passwords.txt", Final);
                FillListViewFromFile();
                NameTextBox.Text = "";
                PasswordTextBox.Text = "";
                URLTextBox.Text = "";
                CommentTextBox.Text = "";
            }

        }

        //
        // KODAVIMAS STRINGO
        //
        private string Encode(string text, byte[] key)
        {
            byte[] Text = Encoding.UTF8.GetBytes(text);
            RijndaelManaged aes = new RijndaelManaged();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            aes.KeySize = 128;
            using (ICryptoTransform encryptor = aes.CreateEncryptor(key, null))
            {
                byte[] encryptedText = encryptor.TransformFinalBlock(Text, 0, Text.Length);
                encryptor.Dispose();
                return Convert.ToBase64String(encryptedText);
            }
        }

        //
        // ATKODAVIMAS STRINGO
        //
        private string Decode(string text, byte[] key)
        {
            byte[] encodedText = Convert.FromBase64String(text);
            RijndaelManaged aes = new RijndaelManaged();
            aes.KeySize = 128;
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.ECB;
            using (ICryptoTransform decryptor = aes.CreateDecryptor(key, null))
            {
                byte[] originalText = decryptor.TransformFinalBlock(encodedText, 0, encodedText.Length);
                decryptor.Dispose();
                return Encoding.UTF8.GetString(originalText);
            }
        }

        //
        // PASIRINKTO SLAPTAZODZIO IRASYMAS I CLIPBOARD
        // tiesiog nukopijuoja slaptazodzio vieta isardzius visa linija is listview
        //
        private void CopyButton_Click(object sender, EventArgs e)
        {
            string fullString = PasswordsListView.SelectedItems[0].Text;
            string[] stringList = fullString.Split(" // ");
            string passwordSnippet = stringList[1];
            Clipboard.SetText(passwordSnippet);
        }

        //
        // PAIESKA
        //
        private void SearchButton_Click(object sender, EventArgs e)
        {
            int counter = 0;
            string line;

            using System.IO.StreamReader file =
                new System.IO.StreamReader(path + "passwords.txt");
            while ((line = file.ReadLine()) != null)
            {
                string[] tempList = line.Split("<>");
                string tempGlobalName = tempList[0];
                string tempGlobalPassword = tempList[1];
                string tempGlobalLine = line;
                string url = tempList[2];
                string comment = tempList[3];
                //
                // KAI RANDA TAI ISVEDA TA EILUTE
                //
                if(SearchTextBox.Text == tempGlobalName)
                {
                    globalName = tempList[0];
                    globalPassword = tempList[1];
                    globalLine = line;
                    EncryptedPasswordLabel.Text = globalPassword;
                    SearchResultLabel.Text = line;
                    RevealPasswordLabel.Text = "[password goes here]";
                }
                counter++;
            }
        }

        //
        // SLAPTAZODZIO PAKEITIMAS
        // iraso ir i faila ir uzkoduoja dar tuo paciu
        //
        private void ChangePasswordButton_Click(object sender, EventArgs e)
        {
            byte[] KeyAES = Encoding.UTF8.GetBytes("testtesttesttest");
            string newPasswordEncoded = Encode(ChangePasswordTextBox.Text,KeyAES);
            File.WriteAllText(path + "passwords.txt", File.ReadAllText(path + "passwords.txt").Replace(globalPassword,newPasswordEncoded));
            FillListViewFromFile();
        }

        //
        // SLAPTAZODZIO TRINIMAS
        // istrina ir is lenteles ir is failo
        //
        private void DeleteButton_Click(object sender, EventArgs e)
        {
            File.WriteAllText(path + "passwords.txt", File.ReadAllText(path + "passwords.txt").Replace(globalLine + "\n", ""));
            FillListViewFromFile();
            SearchResultLabel.Text = "Password was deleted.";
        }

        //
        // ATKODUOJA SLAPTAZODI
        // zinau, kad matosi slaptazodis virsuje listviewe, bet cia is globalaus
        // kintamojo paduoto uzkoduoto perduoda ir atkoduoja slaptazodi
        //
        private void RevealPasswordButton_Click(object sender, EventArgs e)
        {
            byte[] KeyAES = Encoding.UTF8.GetBytes("testtesttesttest");
            RevealPasswordLabel.Text = Decode(globalPassword, KeyAES);
        }

        //
        // SUGENERUOJA RANDOM SLAPTAZODI
        //
        private void RandomPasswordButton_Click(object sender, EventArgs e)
        {
            Guid g = Guid.NewGuid();
            string GuidString = Convert.ToBase64String(g.ToByteArray());
            //
            // PATVARKO SLAPTAZODI
            //
            GuidString = GuidString.Replace("=", "");
            GuidString = GuidString.Replace("+", "");
            GuidString = GuidString.Replace("/", "");
            PasswordTextBox.Text = GuidString;
        }
    }
}

