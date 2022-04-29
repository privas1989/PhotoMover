using log4net;
using log4net.Config;
using SimpleImpersonation;
using System;
using System.IO;
using System.Reflection;

namespace PhotoMover
{
    public class NetworkShare
    {  
        private string domain;
        private string username;
        private string password;
        private string share_path;
        private UserCredentials credentials;
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public NetworkShare(string domain, string username, string password, string share_path, ILog logger)
        {
            this.domain = domain;
            this.username = username;
            this.password = password;
            this.share_path = share_path;
            this.credentials = new UserCredentials(this.domain, this.username, this.password);
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        }

        public bool AddPhoto(string file_name, string source_file_location)
        {
            bool add_result = false;

            DateTime local_last_write_time = new FileInfo(source_file_location).LastWriteTime.ToLocalTime();
            local_last_write_time = RoundUp(local_last_write_time, TimeSpan.FromMinutes(1));

            if (!FileExists(this.share_path + "\\" + file_name.ToLower()))
            {
                try
                {
                    CopyFile(source_file_location, this.share_path + "\\" + file_name);
                    add_result = true;
                    log.Info("Adding " + file_name);  
                }
                catch (Exception e)
                {
                    log.Error(e.Message);
                }
            }
            else
            {
                DateTime dest_last_write_time = GetDestDT(this.share_path + "\\" + file_name.ToLower());
                dest_last_write_time = RoundUp(dest_last_write_time, TimeSpan.FromMinutes(1));

                if (local_last_write_time > dest_last_write_time)
                {
                    try
                    {
                        CopyFile(source_file_location, this.share_path + "\\" + file_name);
                        add_result = true;
                        log.Info("Updating " + file_name);
            
                    }
                    catch (Exception e)
                    {
                        log.Error(e.Message);
                    }
                }
                else
                {
                    add_result = true;
                    log.Debug("Skipping " + file_name);
                }
            }

            return add_result;
        }

        private DateTime RoundUp(DateTime dt, TimeSpan d)
        {
            return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }

        private void CopyFile(string sourceFilePath, string destinationFilePath)
        {
            var content = File.ReadAllBytes(sourceFilePath);
            DateTime local_file_lwt = File.GetLastWriteTime(sourceFilePath);

            Impersonation.RunAsUser(this.credentials, LogonType.NewCredentials, () =>
            {
                File.WriteAllBytes(destinationFilePath, content);
                File.SetLastWriteTime(destinationFilePath, local_file_lwt);
            });
        }

        private bool FileExists(string file_path)
        {
            bool exists = false;

            Impersonation.RunAsUser(this.credentials, LogonType.NewCredentials, () =>
            {
                if (File.Exists(file_path))
                {
                    exists = true;
                }
            });

            return exists;
        }

        private DateTime GetDestDT(string file_path)
        {
            DateTime dt = DateTime.Now;

            Impersonation.RunAsUser(this.credentials, LogonType.NewCredentials, () =>
            {
                dt = new FileInfo(file_path).LastWriteTime.ToLocalTime();
            });

            return dt;
        }
    }
}
