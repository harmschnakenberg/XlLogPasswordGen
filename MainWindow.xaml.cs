using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Reflection;

namespace XlLogPasswordGen
{
    /*
     * Konfig-Datei:
     * Passwort im Klartext, oder Codiert in doppelten Anführunsgzeichen
     * 
     */

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml    
    /// </summary>
    public partial class MainWindow : Window
    {

        private const string ConfigFileName = "XlConfig.ini";
        static readonly string appDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        static readonly string configPath = System.IO.Path.Combine(appDir, ConfigFileName);
        private const string configKey = "XlPassword";
        private static Dictionary<string, string> dict;
        string[] configLines;

        private const int EncryptionKey = 200;

        public MainWindow()
        {
            InitializeComponent();

            dict = LoadConfigFile(configPath);

            if (dict == null)
            {
                Close();
            } 
            else if (!dict.ContainsKey(configKey))
            {
                string message = "Passwort nicht freigegeben in Konfiguartionsdatei. Passwort neu setzen?";
                MessageBoxResult r = MessageBox.Show(message, "Fehler beim Laden", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);

                if (r != MessageBoxResult.Yes)
                {
                    Close();
                }

                dict.Add(configKey, string.Empty);

                Button_Passwort_Validate.IsEnabled = false;
                Button_SetNewPassword.IsEnabled = true;
                GroupBox_Passwort.Header = "neues Passwort:";
                TextBlock_Fortschritt.Text = "Neues Passwort eingeben...";
                ProgressBar_Fortschritt.Value += 2;
            }
            else
            {
                TextBlock_Fortschritt.Text = "Altes Passwort eingeben...";
                ProgressBar_Fortschritt.Value += 1;
            }
        }

        private Dictionary<string, string> LoadConfigFile(string configPath)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();

            if (!File.Exists(configPath))
            {
                string message = string.Format("Konfiguartionsdatei nicht gefunden:\r\n" +
                    "Stellen Sie sicher, dass sich die Konfigurationsdatei \r\n" +
                    "'{0}' \r\n" +
                    "im gleichen Ordner befindet wie diese Anwendung\r\n" +
                    "und versuchen Sie es erneut.", ConfigFileName);
                MessageBox.Show(message, "Fehler beim Laden", MessageBoxButton.OK, MessageBoxImage.Error);

                return null;
            }
            else
            {
                try
                {
                    string configAll = System.IO.File.ReadAllText(configPath, System.Text.Encoding.UTF8);
                    char[] delimiters = new char[] { '\r', '\n' };
                    configLines = configAll.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in configLines)
                    {
                        if (line[0] != ';' && line[0] != '[')
                        {
                            string[] item = line.Split('=');
                            string val = item[1].Trim();
                            if (item.Length > 2) val += "=" + item[2].Trim();
                            dict.Add(item[0].Trim(), val);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new IOException("Fehler beim Einlesen der Konfigurationsdatei\r\n" + configPath + "\r\n" + ex.Message);
                }
            }
        
            return dict;
        }

        public string EncryptDecrypt(string szPlainText, int szEncryptionKey)
        {
            StringBuilder szInputStringBuild = new StringBuilder(szPlainText);
            StringBuilder szOutStringBuild = new StringBuilder(szPlainText.Length);
            char Textch;
            for (int iCount = 0; iCount < szPlainText.Length; iCount++)
            {
                Textch = szInputStringBuild[iCount];
                Textch = (char)(Textch ^ szEncryptionKey);
                szOutStringBuild.Append(Textch);
            }
            return szOutStringBuild.ToString();
        }

        private void Button_Passwort_Validate_Click(object sender, RoutedEventArgs e)
        {
            string plainPassword = TextBox_Passwort.Text;
            string raw = dict[configKey];
            bool passwordOk = false;

            if (raw.StartsWith("\"") && raw.EndsWith("\""))
            {
                //Codiertes Passwort steht in doppelten Anführungszeichen
                string encrypedPassword = raw.Substring(1, raw.LastIndexOf('"') - 1);
               
                if (encrypedPassword == EncryptDecrypt(plainPassword, EncryptionKey))                
                    passwordOk = true;
            }
            else
            {
                //Passwort in Konfig als Klartext
                if (raw == plainPassword && raw.Length > 3)                
                    passwordOk = true;                
            }

            if (!passwordOk)
            {
                string message = string.Format("Das eingegebene Passwort \r\n'{0}'\r\nstimmt nicht mit dem alten Passwort überein.", plainPassword);
                _ = MessageBox.Show(message, "Passwort falsch", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else
            {
                GroupBox_Passwort.Header = "Neues Passwort:";
                Button_Passwort_Validate.IsEnabled = false;
                Button_SetNewPassword.IsEnabled = true;
                TextBlock_Fortschritt.Text = "Neues Passwort eingeben...";
                ProgressBar_Fortschritt.Value += 1;
            }
        }

        private void Button_SetNewPassword_Click(object sender, RoutedEventArgs e)
        {
            string plainPassword = TextBox_Passwort.Text;

            string encrypedPassword = EncryptDecrypt(plainPassword, EncryptionKey);

            bool isNewPwSet = false;

            if (encrypedPassword.Length < 3)
            {
                string message = string.Format("Das eingegebene Passwort \r\n'{0}'\r\nist zu kurz. Bitte wählen Sie ein anderes Passwort.", plainPassword);
                _ = MessageBox.Show(message, "Passwort zu kurz", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string @string = string.Empty;

            foreach (string line in configLines)
            {
                if (line.StartsWith(configKey))
                {
                    @string += configKey + "=\"" + encrypedPassword + "\"\r\n";
                    isNewPwSet = true;
                }
                else
                {
                    if (line.StartsWith("["))
                    {
                        @string += "\r\n";
                    }

                    @string += line + "\r\n";
                }
            }

            if (!isNewPwSet)
            {
                @string += "\r\n" + configKey + "=\"" + encrypedPassword + "\"";
            }

            try
            {
                File.WriteAllText(configPath, @string, Encoding.UTF8);

                string message = "Das Passwort für Excel-Tabellen wurde erfolgreich neu gesetzt.";
                _ = MessageBox.Show(message, "Passwort neu gesetzt", MessageBoxButton.OK, MessageBoxImage.Information);

                Close();
            }
            catch
            {
                throw new IOException("Die Konfigurationsdatei konnte nicht überschrieben werden.");
            }
        }
    }
}