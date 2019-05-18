using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlogaoPhotoBackup {
    class Program {

        const string FLOGAO_PHOTOS_LIST_URL = "https://www.flogao.com.br/{0}/photos/{1}";
        const string FLOGAO_PHOTO_URL = "https://cache-assets.flogao.com.br/photos/full/{0}.jpg";

        static void Main(string[] args) {
            
            if (args.Length != 1) {
                Console.WriteLine("Invalid arguments.");
                Console.ReadLine();
                return;
            }

            Main(args[0]).GetAwaiter().GetResult();
        }

        static async Task Main(string flogaoName) {

            string localPhotosFolder = $"{Environment.CurrentDirectory}\\{flogaoName}-photos";
            Directory.CreateDirectory(localPhotosFolder);

            string url = string.Format(FLOGAO_PHOTOS_LIST_URL, flogaoName, 1);

            Console.WriteLine("Getting Flogao photos page HTML code...");
            string html = await GetPageHtml(url);

            if (string.IsNullOrEmpty(html)) {
                Console.WriteLine("Failed to find the photos page, try again later.");
                Console.ReadLine();
                return;
            }

            var pageCount = GetPhotosPagesCount(html);

            Console.WriteLine($"Finding photos list elements from all {pageCount} pages...");
            var photoIds = await GetPhotosIds(flogaoName, pageCount);

            Console.WriteLine($"Found {photoIds.Count} photos to download.");
            Console.WriteLine("Downloading photos...");

            int index = 1;
            photoIds.ForEach(id => {
                Console.Write($"\rDownloading photo {index} of {photoIds.Count}.");
                DownloadPhoto(id, localPhotosFolder);
                index++;
            });

            Console.WriteLine("\nPhotos Saved!");
            Console.ReadLine();
        }

        static async Task<string> GetPageHtml(string url) {
            using (var httpClient = new HttpClient()) {
                using (var response = await httpClient.GetAsync(url)) {
                    if (response.IsSuccessStatusCode) {
                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }

            return string.Empty;
        }

        static int GetPhotosPagesCount(string html) {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            int maxPage = 1;
            var nodes = doc.DocumentNode.SelectNodes("//div[@class='Pages']//div[@class='Paginator']//a");
            foreach(var node in nodes) {
                int.TryParse(node.InnerText, out int pageNum);
                if (pageNum > 0) {
                    maxPage = pageNum > maxPage ? pageNum : maxPage;
                }
            }

            return maxPage;
        }

        static async Task<List<string>> GetPhotosIds(string flogaoName, int pageCount) {
            var photoIds = new List<string>();
            for (int page = 1; page <= pageCount; page++) {

                string url = string.Format(FLOGAO_PHOTOS_LIST_URL, flogaoName, page);
                string html = await GetPageHtml(url);

                var photoListElements = FindPhotosElements(html);
                foreach (var element in photoListElements) {
                    var elementValue = element.Attributes["href"].Value;
                    var imgId = Regex.Replace(elementValue, "\\D", "");
                    photoIds.Add(imgId);
                }
            }
            return photoIds;
        }

        static HtmlNodeCollection FindPhotosElements(string html) {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            return doc.DocumentNode.SelectNodes("//div[@class='UserStream']//div[@class='user vcard']//div//a");
        }

        static void DownloadPhoto(string photoId, string photoFolder) {
            using (var webClient = new WebClient()) {
                string photoUrl = string.Format(FLOGAO_PHOTO_URL, photoId);
                string photoPath = $"{photoFolder}\\{photoId}.jpg";

                webClient.DownloadFile(new Uri(photoUrl), photoPath);
            }
        }
    }
}
