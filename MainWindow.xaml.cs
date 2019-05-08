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
using Microsoft.VisualBasic.FileIO;
using System.Windows.Forms;

namespace WallpaperSorter
{
    public class ParentEntry
    {
        public ParentEntry(string path, int index)
        {
            Path = path;
            Index = index;
        }

        public string Path;
        public int Index;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> fileList;
        int fileIndex;

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        private void AddFiles(DirectoryInfo dirInfo)
        {
            foreach (FileInfo file in dirInfo.GetFiles())
            {
                string ext = file.Extension.ToLower();
                if ((ext == ".jpg") ||
                    (ext == ".bmp") ||
                    (ext == ".png") ||
                    (ext == ".gif"))
                {
                    fileList.Add(file.FullName);
                }
            }

            foreach (DirectoryInfo dir in dirInfo.GetDirectories())
            {
                AddFiles(dir);
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Right)
                Traverse(true);
            else if (e.Key == Key.Left)
                Traverse(false);
            else if (e.Key == Key.Escape)
                Init();
            else if (e.Key == Key.Delete)
            {
                FileSystem.DeleteFile(fileList[fileIndex], UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                bool result = Traverse(true);
                if (!result)
                    Traverse(false);
            }
        }

        void Init()
        {
            fileList = new List<string>();

            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.ShowNewFolderButton = true;
            folderBrowserDialog.Description = "Select the directory that you want to check.";

            System.Windows.Forms.DialogResult result = folderBrowserDialog.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            string folderName = folderBrowserDialog.SelectedPath;

            if (string.IsNullOrEmpty(folderName))
                return;

            DirectoryInfo dirInfo = new DirectoryInfo(folderName);
            AddFiles(dirInfo);

            fileIndex = -1;
            Traverse(true);
        }

        bool Traverse(bool forward)
        {
            int currentIndex = fileIndex;

            if (forward)
            {
                if ((fileIndex + 1) == fileList.Count)
                    return false;

                fileIndex++;
            }
            else
            {
                if (fileIndex == 0)
                    return false;

                fileIndex--;
            }

            if (File.Exists(fileList[fileIndex]))
                ShowImage(fileList[fileIndex]);
            else
            {
                bool result = Traverse(forward);
                if (!result)
                    fileIndex = currentIndex;

                return result;
            }

            return true;
        }

        void ShowImage(string filename)
        {
            try
            {
                //get the screen dimensions and calculate the width of the task bar
                System.Drawing.Rectangle totalScreenSize = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                System.Drawing.Rectangle workingScreenSize = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;

                int lowerBorder = totalScreenSize.Height - workingScreenSize.Height;
                Int32Rect desiredImageSize = new Int32Rect(0, 0, totalScreenSize.Width, workingScreenSize.Height);
                Int32Rect actualImageSize = new Int32Rect(0, 0, 0, 0);
                Int32Rect finalImageSize = new Int32Rect(0, 0, 0, 0);
                PixelFormat imgFormat;
                BitmapPalette imgPalette;
                int imgScaleFactor = 4;

                //get the size and details of the actual image
                {
                    BitmapImage tmp_img = new BitmapImage();
                    tmp_img.BeginInit();
                    tmp_img.CreateOptions = BitmapCreateOptions.None;
                    tmp_img.CacheOption = BitmapCacheOption.OnLoad;
                    tmp_img.UriSource = new Uri(filename);
                    tmp_img.EndInit();

                    actualImageSize.Width = tmp_img.PixelWidth;
                    actualImageSize.Height = tmp_img.PixelHeight;
                    imgFormat = tmp_img.Format;
                    imgPalette = tmp_img.Palette;
                    imgScaleFactor = imgFormat.BitsPerPixel / 8;
                }

                //create the final bitmap which we will write to the file
                WriteableBitmap bmp = new WriteableBitmap(totalScreenSize.Width, totalScreenSize.Height, 96, 96, imgFormat, imgPalette);

                //set the background of the final image
                {
                    List<Color> colors = new List<Color>();
                    colors.Add(Colors.Black);
                    BitmapPalette palette = new BitmapPalette(colors);

                    // Creates a new empty image with the pre-defined palette
                    byte[] pixels = new byte[totalScreenSize.Width * totalScreenSize.Height * imgScaleFactor];
                    BitmapSource source = BitmapSource.Create(totalScreenSize.Width, totalScreenSize.Height, 96, 96, PixelFormats.Indexed1, palette, pixels, totalScreenSize.Width * imgScaleFactor);
                    FormatConvertedBitmap converted = new FormatConvertedBitmap(source, imgFormat, imgPalette, 0);

                    converted.CopyPixels(pixels, totalScreenSize.Width * imgScaleFactor, 0);
                    Int32Rect background_srcRect = new Int32Rect(0, 0, totalScreenSize.Width, totalScreenSize.Height);
                    bmp.WritePixels(background_srcRect, pixels, totalScreenSize.Width * imgScaleFactor, 0);
                }

                //get the image in the corrected dimension space
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.CreateOptions = BitmapCreateOptions.None;
                img.CacheOption = BitmapCacheOption.OnLoad;

                if (desiredImageSize.Width > actualImageSize.Width)
                {
                    if (desiredImageSize.Height >= actualImageSize.Height)
                    {
                        //image is smaller than the screen size
                        //we don't need to do anything here
                    }
                    else
                    {
                        //need to shrink the height of the image
                        img.DecodePixelHeight = desiredImageSize.Height;
                    }
                }
                else
                {
                    if (desiredImageSize.Height >= actualImageSize.Height)
                    {
                        //need to shrink the width of the image
                        img.DecodePixelWidth = desiredImageSize.Width;
                    }
                    else
                    {
                        //image is totally larger than the screen size
                        if (((double)actualImageSize.Height / (double)desiredImageSize.Height) > ((double)actualImageSize.Width / (double)desiredImageSize.Width))
                            img.DecodePixelHeight = desiredImageSize.Height;
                        else
                            img.DecodePixelWidth = desiredImageSize.Width;
                    }
                }

                img.UriSource = new Uri(filename);
                img.EndInit();

                //recalculate the image offsets on the final bitmap
                finalImageSize.Width = img.PixelWidth;
                finalImageSize.Height = img.PixelHeight;
                finalImageSize.X = (desiredImageSize.Width - finalImageSize.Width) / 2;
                finalImageSize.Y = (desiredImageSize.Height - finalImageSize.Height) / 2;

                //copy the image to the final output
                byte[] img_bytes = new byte[finalImageSize.Width * finalImageSize.Height * imgScaleFactor];
                img.CopyPixels(img_bytes, finalImageSize.Width * imgScaleFactor, 0);

                Int32Rect srcRect = new Int32Rect(0, 0, finalImageSize.Width, finalImageSize.Height);
                bmp.WritePixels(srcRect, img_bytes, finalImageSize.Width * imgScaleFactor, finalImageSize.X, finalImageSize.Y);
                Wallpaper.Source = bmp;
            }
            catch (Exception)
            {
                Wallpaper.Source = null;
            }
        }
    }
}
