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


namespace TagS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string []files = { "-1" };
        int count = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Choose_Directory(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDlg = new OpenFileDialog(); //Create a new FileDialog
            openDlg.Multiselect = true;//Allow the user to select multiple files
            openDlg.Filter = "MP3 Files (*.mp3) |*.mp3"; //Allow only .pdf's
            openDlg.ShowDialog();
            files = openDlg.FileNames;

            if (openDlg.FileNames.Length != 0)
                AutoFillFields();


        }

        private void SetTags(object sender, RoutedEventArgs e)
        {
            if(files[0] == "-1")
            {
                System.Windows.MessageBox.Show("Please select music files to examine...", "Error!");
                return;
            }
            if (count < files.Length - 1)
            {


                var file = TagLib.File.Create(files[count++]); //Open the file

                file.Tag.Title = songtitle.Text;
                file.Tag.Album = album.Text;
                file.Tag.Performers = new String[1] { artist.Text };
                if (year.Text.Length != 0)
                    file.Tag.Year = Convert.ToUInt32(year.Text);
                else
                    file.Tag.Year = Convert.ToUInt32(DateTime.Now.Year);
                file.Tag.Genres = new String[1] { genre.Text };


                file.Save();

                EmptyTextFields();
                AutoFillFields();


            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)//Allow only numbers
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        public void EmptyTextFields()
        {
            songtitle.Text = "";
            album.Text = "";
            artist.Text = "";
            year.Text = "";
            genre.Text = "";
        }
        public void AutoFillFields()
        {
            var songname = System.IO.Path.GetFileNameWithoutExtension(files[count]);
            filename.Content = "File Name : " + System.IO.Path.GetFileName(files[count]); //Display the file name on the header


            counter.Text = (count+1).ToString() + '/' + files.Length.ToString();

            //Check in case there are no dashes
            if (songname.LastIndexOf('-') >= 0)
                songtitle.Text = songname.Substring(songname.LastIndexOf('-') + 2); //Text after '-' , '+2' so that there isn't whitespace before the string
            else
                songtitle.Text = songname;


            if (songname.IndexOf('-') >= 0)
                artist.Text = songname.Substring(0, songname.IndexOf('-') - 1); //Text before it , likewise '-1'

            if (count + 1 == files.Length)
                next.Content = "Finish";
            else
                next.Content = "Next";

        }

        private void BackButton(object sender, RoutedEventArgs e)
        {
            if(count >= 1)
            {
                count--;
                EmptyTextFields();
                AutoFillFields();
            }
            else
            {
                System.Windows.MessageBox.Show("There are no previous files");
            }

        }


        private void GotoNext(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.F2))
                SetTags(sender,e);
            else if(e.Key == Key.F1)
                BackButton(sender,e);
        }
    }



}
