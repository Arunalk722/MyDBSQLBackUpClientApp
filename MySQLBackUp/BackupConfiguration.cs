using Google.Protobuf.WellKnownTypes;
using MySql.Data.MySqlClient;
using MySQLBackUp;
using MySQLBackUp.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MySQLBackUp
{
    public partial class Backup : Form
    {
        public Backup()
        {
            InitializeComponent();
        }
        String backupFileName,folderName = "";

        private void btnRun_Click(object sender, EventArgs e)
        {
            
                connectionEstablish();                
            
        }
        public static MySqlConnection connection = null;
        private void connectionEstablish()
        {

            try
            {
            int maxConnections = 1500;  // Set the desired maximum connections

            for (int i = 1; i <= maxConnections; i++)
            {
                connection = new MySqlConnection("server=127.0.0.1;port=3306;database=sakila;uid=user;password=password;");
                connection.Open();
                Console.WriteLine("Connected to MySQL database!");
                lblStatus.Text = "Connectd" + i.ToString();    

                }
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.ToString());
            }

        }
        private void btnbackup_Click(object sender, EventArgs e)
        {
            startBackup();
        }

        private void btnSaveConfig_Click(object sender, EventArgs e)
        {


            
            if (!File.Exists(txtZipDestination.Text))
            {                  
                Directory.CreateDirectory(txtZipDestination.Text);             
            }
            if (!File.Exists(txtDestination.Text))
            {   
                Directory.CreateDirectory(txtDestination.Text);
            }
            Properties.Settings.Default.ServerName = txtServerName.Text;
            Properties.Settings.Default.Port = Convert.ToInt32(txtPort.Text);
            Properties.Settings.Default.DatabaseName = txtDatabaseName.Text;
            Properties.Settings.Default.UserName = txtDBUserName.Text;
            Properties.Settings.Default.Password = txtPassword.Text;
            Properties.Settings.Default.DestinationTo = txtDestination.Text;
            Properties.Settings.Default.Time = txtRunTime.Text;
            Properties.Settings.Default.SMSAddress = txtSMSURL.Text;
            Properties.Settings.Default.SMSTo = txtSMSTo.Text;
            Properties.Settings.Default.ZipDestination = txtZipDestination.Text;
            Properties.Settings.Default.Save();
            MessageBox.Show("Setting Saved!");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
           
           
            txtServerName.Text = Properties.Settings.Default.ServerName;
            txtPort.Text = Properties.Settings.Default.Port.ToString();
            txtDatabaseName.Text = Properties.Settings.Default.DatabaseName.ToString();
            txtDBUserName.Text = Properties.Settings.Default.UserName.ToString();
            txtPassword.Text = Properties.Settings.Default.Password.ToString();
            txtDestination.Text = Properties.Settings.Default.DestinationTo.ToString();
            txtRunTime.Text = Properties.Settings.Default.Time.ToString();
            txtSMSURL.Text = Properties.Settings.Default.SMSAddress.ToString();
            txtSMSTo.Text = Properties.Settings.Default.SMSTo.ToString();
            txtZipDestination.Text = Properties.Settings.Default.ZipDestination.ToString();
            logWrite("Service Start At :" + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
            logWrite("Last Run time :" + Properties.Settings.Default.LastRunTime.ToString());
            startTimer.Start();
        }

        private void logWrite(String text)
        {
            String logText = text + ":(log Time " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ")\n";
            txtServerLog.AppendText(logText);
            EventTrace.Tracrt(logText);
        }

        private void startBackup()
        {
            try
            {
                folderName = DateTime.Now.ToString("dd_MM_yyyy");

                string constring = "server=" + txtServerName.Text + ";port=" + Convert.ToInt32(txtPort.Text) + ";database=" + txtDatabaseName.Text + ";uid=" + txtDBUserName.Text + ";password=" + txtPassword.Text + ";";
                backupFileName = txtDatabaseName.Text+DateTime.Now.ToString("ddMMyyyyHHmmss");
                string file = @""+ txtDestination.Text+ "\\" + folderName + "\\" + backupFileName + ".sql";
                using (MySqlConnection conn = new MySqlConnection(constring))
                {
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        using (MySqlBackup mb = new MySqlBackup(cmd))
                        {
                            cmd.Connection = conn;
                            conn.Open();
                            mb.ExportToFile(file);
                            conn.Close();
                           String mg = dbToZip(file);
                             if(mg == "OK")
                            {
                                File.Delete(file);
                                logWrite("TEMP FILE REMOVED FILE NAME :"+file);
                            }
                            else
                            {
                                logWrite("COMPRESSING ERROR.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                ExceptionLogging.SendErrorToText(ex);
            }
        }
        private string dbToZip(String file)
        {
            try
            {
                if (!File.Exists(txtZipDestination.Text))
                {
                    //log path create           
                    Directory.CreateDirectory(txtZipDestination.Text);
                }
                string sourceFolderPath = @"" + txtDestination.Text+"\\" + folderName ;
                string zipFilePath = @"" + txtZipDestination.Text + "\\" + backupFileName + ".zip";
                // Create a new zip archive
                ZipFile.CreateFromDirectory(sourceFolderPath, zipFilePath);
                EventTrace.Tracrt("Zip file created.");
                smsSend("Backup Successful. database :" + txtDatabaseName.Text + " at " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"), txtSMSTo.Text);
                Properties.Settings.Default.LastRunTime = lblLocalTime.Text;
                Properties.Settings.Default.Save();

                return "OK";
               
            }
            catch (Exception ex)
            {
                ExceptionLogging.SendErrorToText(ex);
                return "FAILD";
            }
        }

        private void startTimer_Tick(object sender, EventArgs e)
        {
            lblLocalTime.Text = DateTime.Now.ToString("HH:mm:ss");
            if(txtRunTime.Text == lblLocalTime.Text)
            {
                startBackup();
            }
            else
            {

            }
        }
        private void smsSend(String msg,String smsTo)
        {
          
            string message = msg;
            string receiver = smsTo;

            // Construct the complete URL with query string
            var fullUrl = $"{txtSMSURL.Text}+{message}&Recever={receiver}";
            logWrite(fullUrl.ToString());
            // Create an HttpWebRequest object
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fullUrl);
            request.Method = "GET";

            try
            {
                // Get the response from the request
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                // Check the response status
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    logWrite("SMS sent successfully.");
                }
                else
                {
                    logWrite($"Failed to send SMS. Status code: {response.StatusCode}");
                }

                response.Close();
            }
            catch (Exception ex)
            {
               logWrite($"An error occurred: {ex.Message}");
               ExceptionLogging.SendErrorToText(ex);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
          
        }
    }
}
public class EventTrace 
{
        public static void Tracrt(string msg)
           {
               try
               {
                   string date = DateTime.Now.ToString("MM-dd-yyyy");
                   string name = DateTime.Today.ToString("MM-dd-yyyy") + ".hbiz";
                   string directoryPath = MySQLBackUp.Properties.Settings.Default.TraceLog + date;
                   if (!File.Exists(directoryPath))
                   {
                       //log path create           
                       Directory.CreateDirectory(directoryPath);

                   }
                   using (StreamWriter sw = File.AppendText(directoryPath + "\\" + name))
                   {
                       string time = DateTime.Now.ToString("HH:mm:ss");                      
                       string hostname = Environment.MachineName;                      
                      string  TraceLogs = msg + "|\t" + date + "|\t" + time + "|\t" + hostname + "\t|\n";
                       sw.WriteLine(TraceLogs);
                       sw.Flush();
                       sw.Close();
                   }
               }
               catch (Exception ex)
               {
                   ExceptionLogging.SendErrorToText(ex);                    
               }
           }
}
class ExceptionLogging
{

    private static String exeLineNo, exeTitle, exeHeader, exeDescryp, exeShortLog;
    public static void SendErrorToText(Exception ex)
    {
        var line = Environment.NewLine + Environment.NewLine;

        exeLineNo = ex.StackTrace.Substring(ex.StackTrace.Length - 7, 7);
        exeTitle = ex.GetType().Name.ToString();
        exeHeader = ex.GetType().ToString();
        exeDescryp = ex.ToString();
        exeShortLog = ex.Message.ToString();
        try
        {

            string MM = DateTime.Now.ToString("MMMM");
            string DD = DateTime.Now.ToString("dd");
            string YYYY = DateTime.Now.ToString("yyyy");
            string name = "Error.hbiz";
            string date2 = DateTime.Now.ToString("MM-dd-yyyy");
            string directoryPath = MySQLBackUp.Properties.Settings.Default.TraceLog + date2;

            if (!File.Exists(directoryPath))
            {
                //log path create           
                Directory.CreateDirectory(directoryPath);
            }
            using (StreamWriter sw = File.AppendText(directoryPath + "\\" + name))
            {
                string error = "------------ Exception Details on " + " " + DateTime.Now.ToString() + "------------" + line + "Log Written Date:\t" + " " + DateTime.Now.ToString() + line + "Error Line No:\t" + " " + exeLineNo + line + "Error Message Title:\t" + " " + exeTitle + line + "Exception Header:\t" + " " + exeHeader + line + "Error Short Log:\t" + " " + exeShortLog + line + "Description of exception\t:" + " " + exeDescryp + line + "--------------------------------*End*------------------------------------------";
                sw.WriteLine(error + line);
                sw.Flush();
                sw.Close();               

            }

        }
        catch (Exception e)
        {
            e.ToString();
            Console.WriteLine(ex.ToString());            
        }
    }

}