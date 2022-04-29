using System;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using log4net;
using log4net.Config;
using System.Reflection;

namespace PhotoMover
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\log4net.config"));

            log.Info("Photo mover started.");
            //string conn = ConfigurationManager.AppSettings.Get("ConnectionString");
            //string az_share_name = ConfigurationManager.AppSettings.Get("AzureShareName");
            //string az_photos_folder = ConfigurationManager.AppSettings.Get("AzurePhotoDirectory");
            string share_location = "";
            string badge_pass_db = "";
            string domain = "";
            string username = "";
            string password = "";
            string ns_path = ""; 
            Regex photo_regex = new Regex("^\\d{9}\\.jpg");

            try
            {
                share_location = ConfigurationManager.AppSettings.Get("PhotosShare");
                badge_pass_db = ConfigurationManager.AppSettings.Get("BadgePassDatabaseCS");
                domain = ConfigurationManager.AppSettings.Get("NSDomain");
                username = ConfigurationManager.AppSettings.Get("NSUsername");
                password = ConfigurationManager.AppSettings.Get("NSPassword");
                ns_path = ConfigurationManager.AppSettings.Get("NSPath");
                
                log.Info("Configuration loaded.");
            }
            catch (Exception e)
            {
                log.Error("Unable to load configuration. " + e.Message);
            }  

            //AZFileShare fs = new AZFileShare(conn, az_share_name);

            string[] photos = Directory.GetFiles(share_location, "*.jpg", SearchOption.AllDirectories);

            NetworkShare ns = new NetworkShare(domain, username, password, ns_path, log);
            BadgePassDB bp = new BadgePassDB(badge_pass_db);

            foreach (string photo in photos)
            {
                if (photo_regex.IsMatch(Path.GetFileName(photo).ToLower()))
                {   
                    bool uploaded = ns.AddPhoto(Path.GetFileName(photo).ToLower(), photo);
                    //bool uploaded = fs.AZPush(Path.GetFileName(photo).ToLower(), @photo, az_photos_folder);
                    if (!uploaded)
                    {
                        log.Error("Could not copy " + photo);
                    }
                }
                else
                {
                    string guid = Path.GetFileName(photo).Split('.')[0];
                    string id = bp.Translate(guid);

                    if (id != null)
                    {
                        bool uploaded = ns.AddPhoto(id + ".jpg", photo);
                        if (!uploaded)
                        {
                            log.Error("Could not copy " + photo);
                        }
                    }
                }
            }

            log.Info("Photo mover finished.");
        }
    }
}
