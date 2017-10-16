using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;
using System.IO;
using System.Drawing.Imaging;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Globalization;

namespace TagS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string []files = { "-1" }; //Initializer "-1" is used in error prevention
        int count = 0;
        string coverimg_path = "-1";
        TagLib.File file;

        public MainWindow()
        {
            InitializeComponent();
            string path = @".\Thumb_tmp";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            autofillbut.Visibility = Visibility.Hidden; 
        }



        private void Choose_Directory(object sender, RoutedEventArgs e)
        {
            coverimg_path = "";
            OpenFileDialog openDlg = new OpenFileDialog(); //Create a new FileDialog
            openDlg.Multiselect = true;//Allow the user to select multiple files
            openDlg.Filter = "MP3 Files (*.mp3) |*.mp3"; //Allow only .pdf's
            openDlg.ShowDialog();
            files = openDlg.FileNames;



            if (openDlg.FileNames.Length != 0)
            {
                file = TagLib.File.Create(files[count]); //Open the file
                if (!HasTags())
                    AutoFillFields();
                else
                    FillPreExisting();

            }
            


        }

        private void SetTags()
        {
            status.Text = "Status : OK";

            if (files.Length == 0 || files[0] == "-1")
            {
                System.Windows.MessageBox.Show("Please select music files to examine...", "Error!");
                return;
            }
            if (count < files.Length)
            {

                
                file = TagLib.File.Create(files[count]);
                if (coverimg_path != "-1")//If an image has been selected
                {
                    TagLib.Picture pic = new TagLib.Picture(); //Type for the cover art
                    pic.Type = TagLib.PictureType.FrontCover; //Set as cover
                    pic.Description = "Cover Art";
                    pic.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg;

                    if (coverimg_path.StartsWith("http"))//If we got the image from the AutoFill function -- the API
                    {
                        try
                        {
                            pic.Data = TagLib.ByteVector.FromPath(@".\Thumb_tmp\" + songtitle.Text + ".jpg");
                            file.Tag.Pictures = new TagLib.IPicture[1] { pic }; //Add the tag
                        }
                        catch (System.IO.FileNotFoundException)//It has already been deleted
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (coverimg_path.Length != 0)
                        {
                            pic.Data = TagLib.ByteVector.FromPath(coverimg_path);
                            file.Tag.Pictures = new TagLib.IPicture[1] { pic }; //Add the tag
                        }
                    }


                }else if(coverimg.Text.ToLower() == "del")//"Erase" the cover art
                    file.Tag.Pictures = null;


                //Set the rest of the tags
                file.Tag.Title = songtitle.Text;
                file.Tag.Album = album.Text;
                file.Tag.Performers = new String[1] { artist.Text };



                if (tracknum.Text.Length != 0)//If a track number has been given,use it
                    file.Tag.Track = Convert.ToUInt32(tracknum.Text);
                else
                    file.Tag.Track = 0;


                if (year.Text.Length != 0)//If a year has been given, use it
                    file.Tag.Year = Convert.ToUInt32(year.Text);
                else
                    file.Tag.Year = 0;


                //Set Genre
                string[] genreString = Array.ConvertAll(genre.Text.Split(','), Convert.ToString);//Create an array whose elements are the genres we have seperated with commas (',')
                file.Tag.Genres = genreString;

                try
                {
                    file.Save(); //Save the file!
                }
                catch (System.IO.IOException)
                {
                    status.Text = "File is being used , can't save";
                    return;
                }
                count++;

                EmptyTextFields();
                coverimg_path ="-1"; //Reset the image path variable so that the check on the next file works correctly

                if (count < files.Length)
                {
                    if (!HasTags())//If the currect .mp3 file doesn't have tags , auto fill them
                        AutoFillFields();
                    else
                        FillPreExisting();
                }
                else
                {
                    Application.Current.Shutdown(); //Not sure if i can merge this with the one IF below
                    string[] filePaths = Directory.GetFiles(@".\Thumb_tmp\");
                    foreach (string filePath in filePaths)
                        File.Delete(filePath);
                }

            }
            else
            {
                Application.Current.Shutdown();
                string[] filePaths = Directory.GetFiles(@".\Thumb_tmp\");
                foreach (string filePath in filePaths)
                    File.Delete(filePath);
            }
        }


        

        

        public void AutoFillFields()
        {
            var songname = System.IO.Path.GetFileNameWithoutExtension(files[count]);
            filename.Text = "File Name : " + System.IO.Path.GetFileName(files[count]); //Display the file name on the header


            counter.Text = (count+1).ToString() + '/' + files.Length.ToString();

            //Find Artist & Song name by checking the dashes
            //Check in case there are no dashes
            if (songname.LastIndexOf('-') >= 0)
            {
                songtitle.Text = FormatName(songname.Substring(songname.LastIndexOf('-') + 1)); //Text after '-' , '+1' to dismiss the dash
                artist.Text = FormatName(songname.Substring(0, songname.IndexOf('-')));
            }
            else
            {
                songtitle.Text = songname;
                artist.Text = "";
            }


            year.Text = "";
            genre.Text = "";
            album.Text = "";



            //Check the internet for further info -- last.fm api
            string apiKey = ConfigurationManager.AppSettings["APIKey"];//Get the API Key from the config
            status.Text = "Status : Sending Request";
            if (apiKey == null)
            {
                status.Text = "Status : Couldn't make request, incorrect APIKey";
                return;
            }

            string sURL = "http://ws.audioscrobbler.com/2.0/?method=track.getInfo&track="+songtitle.Text +"&artist="+artist.Text+ "&format=json&api_key=" + apiKey;

            //Build the request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(sURL);
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)";

            Console.WriteLine("Requesting : {0}",sURL);
            Stream objStream = null;
            StreamReader objReader;
            JObject json;

            try//to make the request
            {
                objStream = request.GetResponse().GetResponseStream();
            } 
            catch (System.Net.WebException) {
                status.Text = "Status : Connection not established...";
                return;
            }

            //If it is succesfull read the data, turn them into JSON and set the tags
            status.Text = "Status : Request successfull";
            //Read data
            objReader = new StreamReader(objStream);
            //Turn into JSON
            json = JObject.Parse(objReader.ReadToEnd());

            if (json["error"] != null && (int)json["error"] == 6)//If there were no results
            {
                status.Text = "Status : Online request yielded no results...";
                return;
            }

            //Set tags
            if (json["track"]["album"] != null)//If the track belongs in an album, fill the appropriate fields
            {
                //Set album name & track num
                album.Text = (string)json["track"]["album"]["title"];
                string track = null;
                if (json["track"]["album"]["@attr"] != null)
                {
                    track = (string)json["track"]["album"]["@attr"]["position"];
                }
                tracknum.Text = track ?? null;


                //Set thumbnail
                //We download the image thumbnail via WebClient and then set it appropriately
                var webClient = new WebClient();
                string image_link = (string)json["track"]["album"]["image"][3]["#text"];
                if(image_link.Length != 0)
                {
                    byte[] imageBytes = webClient.DownloadData(image_link);
                    img_thumbnail.Source = ToImage(imageBytes);
                    img_thumbnail.Width = 256;
                    img_thumbnail.Height = 256;

                    using (System.Drawing.Image image = System.Drawing.Image.FromStream(new MemoryStream(imageBytes)))
                    {
                       image.Save(@".\Thumb_tmp\" + songtitle.Text + ".jpg", ImageFormat.Jpeg);
                    }



                    coverimg_path = (string)json["track"]["album"]["image"][3]["#text"];
                }

            }

            //Set Genre
            //If there is just one tag
            if(json["track"]["toptags"]["tag"].Count() == 1)
            {
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                genre.Text = textInfo.ToTitleCase((string)json["track"]["toptags"]["tag"][0]["name"]);//Make The First Letter Of Each Word Capital
            }
            //If there more than one tags (set just the first two)
            else if (json["track"]["toptags"]["tag"].Count() > 1)
            {
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
                string firstG = (string)json["track"]["toptags"]["tag"][0]["name"];
                string secG = (string)json["track"]["toptags"]["tag"][1]["name"];

                genre.Text = textInfo.ToTitleCase(firstG +","+ secG);//Make The First Letter Of Each Word Capital
            }

            //Set Artist name
            artist.Text = (string)json["track"]["artist"]["name"];


            if (count + 1 == files.Length)
                next.Content = "Finish[F2]";
            else
                next.Content = "Next[F2]";
        }

    

        private void FillPreExisting()
        {
            file = TagLib.File.Create(files[count]);
            var songname = System.IO.Path.GetFileNameWithoutExtension(files[count]);
            filename.Text = "File Name : " + System.IO.Path.GetFileName(files[count]); //Display the file name on the header

            counter.Text = (count + 1).ToString() + '/' + files.Length.ToString();

            songtitle.Text = file.Tag.Title;

       

            //Read primary tags
            if (file.Tag.Performers.Length > 0)//If there are any performers
                artist.Text = file.Tag.Performers[0];

            if(file.Tag.Year != 0)//If there is a valid year
                year.Text = file.Tag.Year.ToString();

            if (file.Tag.Track != 0)//If there is a valid track#
                tracknum.Text = file.Tag.Track.ToString();

            album.Text = file.Tag.Album;//Assign the Album name either way

            //Read genres
            if (file.Tag.Genres.Length > 0)//If any Genre has been given
            {
                //Build the string "Genre1,Genre2,...."
                int i = 0;
                genre.Text = "";
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

                do
                {
                    if(i > 0)
                        genre.Text += textInfo.ToTitleCase(","+file.Tag.Genres[i]);
                    else
                        genre.Text += textInfo.ToTitleCase(file.Tag.Genres[i]);

                    i++;

                } while (i < file.Tag.Genres.Count());
            }


            //Read cover art
            if (file.Tag.Pictures.Length > 0)
            {
                var bin = file.Tag.Pictures[0].Data.Data; //Create a byte[] out of the tag data
                var convertedimage = ToImage(bin);

                if (convertedimage != null)//If the image has been read successfully
                {
                    img_thumbnail.Source = ToImage(bin);//Make it an image and display it
                    img_thumbnail.Height = 256;
                    img_thumbnail.Width = 256;

                    coverimg.Text = "Type 'Del' to erase the cover";
                }
            }
            else {

                if (img_thumbnail.Source != null) //If there isn't a picture assigned but the image control still has the previous source, flush the control
                {
                    img_thumbnail.Source = null;
                    img_thumbnail.Height = 0;
                    img_thumbnail.Width = 0;
                }
            }

               


            if (count + 1 == files.Length)
                next.Content = "Finish[F2]";
            else
                next.Content = "Next[F2]";


        }



        private void GoBack()
        {
            status.Text = "Status : OK";
            if (count >= 1)
            {
                count--;
                EmptyTextFields();
                coverimg_path = "-1";
                //AutoFillFields();
                FillPreExisting();
            }
            else
            {
                System.Windows.MessageBox.Show("There are no previous files");
            }
        }

        public void EmptyTextFields()
        {
            songtitle.Text = "";
            album.Text = "";
            tracknum.Text = "";
            artist.Text = "";
            year.Text = "";
            genre.Text = "";
            coverimg.Text = "";
        }

        public BitmapImage ToImage(byte[] array)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = new System.IO.MemoryStream(array);
            try
            {
                image.EndInit();
            }
            catch(NotSupportedException e)
            {
                status.Text = "Status : Cover couldn't be retrieved :(";
                return null;
            }
            return image;
        }

        public string FormatName(string str)
        {

            string regex = "(\\[.*\\])|(\".*\")|('.*')|(\\(.*\\))";
            str = Regex.Replace(str, regex, string.Empty); //Remove any delimeters - ({[ - and text in-between

            if (String.IsNullOrWhiteSpace(str))
                return "";


            //If there is whitespace in the first/last position of the name, remove it
            if (str[0] == ' ')
                str = str.Remove(0,1);

            if (str[str.Length - 1] == ' ')
                str = str.Remove(str.Length - 1);


            return str;
        }

        public bool HasTags()
        {

            TagLib.Tag filetags = file.Tag;
            if (filetags.Genres.Length > 0 || Convert.ToBoolean(filetags.Year) || filetags.Performers.Length > 0 || filetags.Title != "" || filetags.Album != "")
            {
                autofillbut.Visibility = Visibility.Visible;
                return true;
            }


            autofillbut.Visibility = Visibility.Hidden;
            return false;
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)//Allow only numbers on the "Year" field
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        


        private void ShortcutKeys(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.F2))
                SetTags();
            else if (e.Key == Key.F1)
                GoBack();
            else if (e.Key == Key.F3)
                AutoFillFields();
        }

        private void ChooseCover(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDlg = new OpenFileDialog(); //Create a new FileDialog
            openDlg.Multiselect = false;
            openDlg.Filter = "JPEG Files(*.jpg) | *.jpg"; 
            openDlg.ShowDialog();

            if(openDlg.FileNames.Length > 0)
                coverimg_path = openDlg.FileNames[0];

            if (coverimg_path != "-1")//Fill the text field with the path
            {
                coverimg.Text = coverimg_path;
                var bin = File.ReadAllBytes(coverimg_path); //Create a byte[] out of the tag data

                img_thumbnail.Source = ToImage(bin);//Make it an image and display it
                img_thumbnail.Height = 256;
                img_thumbnail.Width = 256;
            }
        }



        private void AutoFill_Button(object sender, RoutedEventArgs e)
        {
            AutoFillFields();
        }

        private void Back_Button(object sender, RoutedEventArgs e)
        {
            GoBack();
        }

        private void Next_Button(object sender, RoutedEventArgs e)
        {
            status.Text = "Status : OK";
            SetTags();
        }

        private void CoverImage_Click(object sender, MouseButtonEventArgs e) // CHECK FOR KEYBOARD FOCUS TOO <!>
        {
            if(coverimg.Text == "Type 'Del' to erase the cover")
                coverimg.Text = "";
        }
    }



}
