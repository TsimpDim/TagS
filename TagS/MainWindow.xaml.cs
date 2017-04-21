using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

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
            autofillbut.Visibility = Visibility.Hidden; 
        }



        private void Choose_Directory(object sender, RoutedEventArgs e)
        {
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
            if(files.Length == 0 || files[0] == "-1")
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
                    pic.Data = TagLib.ByteVector.FromPath(coverimg_path);
                    file.Tag.Pictures = new TagLib.IPicture[1] { pic }; //Add the tag

                }else if(coverimg.Text.ToLower() == "del")//"Erase" the cover art
                    file.Tag.Pictures = null;


                //Set the rest of the tags
                file.Tag.Title = songtitle.Text;
                file.Tag.Album = album.Text;
                file.Tag.Performers = new String[1] { artist.Text };
                if (year.Text.Length != 0)//If a year has been given, use it
                    file.Tag.Year = Convert.ToUInt32(year.Text);
                else
                    file.Tag.Year = 0;

                file.Tag.Genres = new String[1] { genre.Text };


                file.Save(); //Save the file!
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
                }

            }
            else
            {
                Application.Current.Shutdown();
            }
        }


        

        

        public void AutoFillFields()
        {
            var songname = System.IO.Path.GetFileNameWithoutExtension(files[count]);
            filename.Text = "File Name : " + System.IO.Path.GetFileName(files[count]); //Display the file name on the header


            counter.Text = (count+1).ToString() + '/' + files.Length.ToString();

            //Check in case there are no dashes
            if (songname.LastIndexOf('-') >= 0)
                songtitle.Text = songname.Substring(songname.LastIndexOf('-') + 2); //Text after '-' , '+2' so that there isn't whitespace before the string
            else
                songtitle.Text = songname;


            if (songname.IndexOf('-') >= 0)
                artist.Text = songname.Substring(0, songname.IndexOf('-') - 1); //Text before it , likewise '-1'
            else
                artist.Text = "";

            if (count + 1 == files.Length)
                next.Content = "Finish[F2]";
            else
                next.Content = "Next[F2]";

            year.Text = "";
            genre.Text = "";
            album.Text = "";

        }

        private void FillPreExisting()
        {
            file = TagLib.File.Create(files[count]);
            var songname = System.IO.Path.GetFileNameWithoutExtension(files[count]);
            filename.Text = "File Name : " + System.IO.Path.GetFileName(files[count]); //Display the file name on the header

            counter.Text = (count + 1).ToString() + '/' + files.Length.ToString();

            songtitle.Text = file.Tag.Title;

       


            if (file.Tag.Performers.Length > 0)//If there are any performers
                artist.Text = file.Tag.Performers[0];

            if(file.Tag.Year != 0)//If there is a valid year
                year.Text = file.Tag.Year.ToString();


            album.Text = file.Tag.Album;//Assign the Album name either way


            if (file.Tag.Genres.Length > 0)//If any Genre has been given
                genre.Text = file.Tag.Genres[0];

            if (file.Tag.Pictures.Length > 0)
            {
                var bin = file.Tag.Pictures[0].Data.Data; //Create a byte[] out of the tag data

                img_thumbnail.Source = ToImage(bin);//Make it an image and display it
                img_thumbnail.Height = 256;
                img_thumbnail.Width = 256;

                coverimg.Text = "Type 'Del' to erase the cover";
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
            if (count >= 1)
            {
                count--;
                EmptyTextFields();
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
            image.EndInit();
            return image;
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
            SetTags();
        }

        private void CoverImage_Click(object sender, MouseButtonEventArgs e) // CHECK FOR KEYBOARD FOCUS TOO <!>
        {
            if(coverimg.Text == "Type 'Del' to erase the cover")
                coverimg.Text = "";
        }
    }



}
