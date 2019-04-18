using Microsoft.Azure.ServiceBus;
using System;
using System.Configuration;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;



namespace ConsoleApplicationBoards
{
    class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        const string ServiceBusConnectionString = "Endpoint=sb://servicebusqu.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=oGyl4NRX/0+u16SBaaHJBMMSrQXL0lwIi34I1xXZRhE=";
        const string QueueName = "firstqueue";
        public string fileName = "";
        public string sourceFile = "";
        public string destFile = "";
        public string sourcePath = "";
        public string destinationPath = "";
        

        static void Main(string[] args)
        {
            ISessionClient sessionClient = null;
            Program c = new Program();


            try
            {
                //settings of session with ConnectionString and Name of queue for cloud side
                sessionClient = new SessionClient(ServiceBusConnectionString, QueueName);
                ReceiveMessages(sessionClient);
                Console.ReadKey();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                ReceiveMessages(sessionClient);
                Console.ReadKey();
            }
            
        }

        private static void ReceiveMessages(ISessionClient sessionClient)
        {
            IMessageSession messageSession = sessionClient.AcceptMessageSessionAsync("Test1").GetAwaiter().GetResult();
            Program c = new Program();

            try
            {
                Message messageReceived = null;

                do
                {
                    log.Info("Receiver listening");
                    messageReceived = messageSession.ReceiveAsync().GetAwaiter().GetResult();

                    if (messageReceived != null)
                    {
                        
                        Console.WriteLine($"Received message: SequenceNumber:{messageReceived.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(messageReceived.Body)}");

                        //this command permit to delete messages from the Cloud service, if the connection lost the application retry to download datas
                        messageSession.CompleteAsync(messageReceived.SystemProperties.LockToken).GetAwaiter().GetResult();

                    }

                    //call of method to move files from a directory to anothe
                    c.ConfigurationSourceDestinationPath("sourcePath", "destinationPath", $"{Encoding.UTF8.GetString(messageReceived.Body)}");
                  
                } while (!messageSession.IsClosedOrClosing);

                Console.WriteLine("Press any key to exit");
                Console.ReadKey();

            }
            finally
            {
                
                messageSession.CloseAsync().GetAwaiter().GetResult();
                Console.ReadKey();

            }
        }

        public void ConfigurationSourceDestinationPath(string source, string destination, string filename)
        {
            sourcePath = ConfigurationManager.AppSettings[source];
            destinationPath = ConfigurationManager.AppSettings[destination];


            //if destination directory not exist, create it
            if (!System.IO.Directory.Exists(destinationPath))
            {
                System.IO.Directory.CreateDirectory(destinationPath);
            }

            // To move all the files in one directory to another directory:

            try
            {
               sourceFile = System.IO.Path.Combine(sourcePath, filename);
                //if file not exist, wait for it
                while (!System.IO.File.Exists(sourceFile)) 
                {
                    //sleep for 20sec 
                    Thread.Sleep(20000);
                }

                    //pass to method GetFiles, the fileName from the message, to search fisically file and move it 
                    string[] files = System.IO.Directory.GetFiles(sourcePath, filename);

                        //Move the files and overwrite destination files if they already exist.
                        foreach (string f in files)
                        {
                            fileName = System.IO.Path.GetFileName(f);
                            
                            destFile = System.IO.Path.Combine(destinationPath, fileName);
                            //if file on destination directory exist already, delete it and then move it again on directory 
                            if(System.IO.File.Exists(destFile))
                            {
                                System.IO.File.Delete(destFile);
                            }   
                            System.IO.File.Move(sourceFile, destFile);
                            Console.WriteLine(fileName + " was moved from " + sourcePath + " to " + destinationPath);

                         }     
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
            }
        }
    }
}
