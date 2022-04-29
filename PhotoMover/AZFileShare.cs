using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PhotoMover
{
    public class AZFileShare
    {
        private string conn;
        private string share_name;

        public AZFileShare(string conn, string share_name)
        {
            this.conn = conn;
            this.share_name = share_name;
        }

        public bool AZPush(string file_name, string file_location, string az_directory)
        {
            bool successful = false;
            IDictionary<string, string> metadata = new Dictionary<string, string>();

            ShareFileClient fl = new ShareFileClient(this.conn, this.share_name, az_directory + "/" + file_name);
            if (!fl.Exists())
            {
                try
                {
                    using (FileStream stream = File.OpenRead(file_location))
                    {
                        metadata.Add("LastWriteTime", new FileInfo(file_location).LastWriteTime.ToLocalTime().ToString());
                        fl.Create(stream.Length);
                        fl.SetMetadata(metadata);
                        fl.UploadRange( new HttpRange(0, stream.Length), stream);
                    }

                    successful = true;
                    Console.WriteLine("Added " + file_name);
                }
                catch (Exception e) { Console.WriteLine(e.Message); }
            }
            else
            {
                DateTime local_last_write_time = new FileInfo(file_location).LastWriteTime.ToLocalTime();
                local_last_write_time = RoundUp(local_last_write_time, TimeSpan.FromMinutes(1));
                ShareFileProperties properties = fl.GetProperties();

                string az_wt = "";
                if (properties.Metadata.TryGetValue("lastwritetime", out az_wt))
                {
                    DateTime az_last_write_time = DateTime.Parse(az_wt);
                    az_last_write_time = RoundUp(az_last_write_time, TimeSpan.FromMinutes(1));
                    
                    if (local_last_write_time > az_last_write_time)
                    {
                        try
                        {
                            using (FileStream stream = File.OpenRead(file_location))
                            {
                                metadata.Add("LastWriteTime", new FileInfo(file_location).LastWriteTime.ToLocalTime().ToString());
                                fl.Create(stream.Length);
                                fl.SetMetadata(metadata);
                                fl.UploadRange(new HttpRange(0, stream.Length), stream);
                            }

                            successful = true;
                            Console.WriteLine("Updated " + file_name);
                        }
                        catch (Exception e) { Console.WriteLine(e.Message); }
                    }
                    else
                    {
                        successful = true;
                    }
                }    
            }
     
            return successful;
        }

        public void DeleteAll(string az_folder)
        {
            ShareDirectoryClient dir = new ShareDirectoryClient(this.conn, this.share_name, az_folder);
            foreach (ShareFileItem item in dir.GetFilesAndDirectories())
            {
                Console.WriteLine(item.Name);
                if(!item.IsDirectory)
                {
                    dir.DeleteFile(item.Name);
                }
            }
        }

        private DateTime RoundUp(DateTime dt, TimeSpan d)
        {
            return new DateTime((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        }
    }
}
