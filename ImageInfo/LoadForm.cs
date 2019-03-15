using MetadataExtractor;
using MetadataExtractor.Formats.Bmp;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.FileSystem;
using MetadataExtractor.Formats.Gif;
using MetadataExtractor.Formats.Jfif;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.Formats.Pcx;
using MetadataExtractor.Formats.Png;
using MetadataExtractor.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageInfo
{
    public partial class LoadForm : Form
    {
        public LoadForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.ShowDialog();
            pathTextBox.Text = folderBrowserDialog.SelectedPath;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var path = pathTextBox.Text;

            if (!System.IO.Directory.Exists(path))
            {
                MessageBox.Show("Invalid path");
                pathTextBox.Text = "";
            }
            else
            {
                new MainForm(ParseFolder(System.IO.Directory.GetFiles(path))).Show();
            }
        }

        private List<FileInfo> ParseFolder(string[] paths)
        {
            var fileInfos = new List<FileInfo>();
            foreach (var path in paths)
            {
                try
                {
                    var info = TryGetFileInfo(path);
                    fileInfos.Add(info);
                }
                catch (ImageProcessingException e)
                {
                    fileInfos.Add(new FileInfo { Name = Path.GetFileName(path) });
                    continue;
                }
            }

            return fileInfos;
        }

        private FileInfo TryGetFileInfo(string path)
        {
            Stream fileStream = new FileStream(path, FileMode.Open);

            FileType fileType = FileTypeDetector.DetectFileType(fileStream);
            switch (fileType)
            {
                case FileType.Jpeg:
                    return FromJpeg(fileStream);
                case FileType.Gif:
                    return FromGif(fileStream);
                case FileType.Tiff:
                    return FromTiff(fileStream);
                case FileType.Bmp:
                    return FromBmp(fileStream);
                case FileType.Png:
                    return FromPng(fileStream);
                case FileType.Pcx:
                    return FromPcx(fileStream);
                case FileType.Unknown:
                default:
                    return new FileInfo { Name = Path.GetFileName(path) };
            }
        }

        private FileInfo FromJpeg(Stream fileStream)
        {
            var directories = ImageMetadataReader.ReadMetadata(fileStream);
            var fileInfo = BaseInfo(fileStream);

            JpegDirectory jpegDirectory = directories.OfType<JpegDirectory>().FirstOrDefault();

            // Resolution
            int w = jpegDirectory.GetInt32(JpegDirectory.TagImageWidth);
            int h = jpegDirectory.GetInt32(JpegDirectory.TagImageHeight);
            fileInfo.Resolution = $"{w}x{h} px";

            // DPI
            var exifDirectory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            if (exifDirectory != null)
            {
                fileInfo.DPI = exifDirectory.GetDescription(ExifIfd0Directory.TagXResolution);
            }
            else
            {
                var jfifDirectory = directories.OfType<JfifDirectory>().FirstOrDefault();
                if (jfifDirectory != null)
                {
                    fileInfo.DPI = jfifDirectory.GetDescription(JfifDirectory.TagResX);
                }
                else
                {
                    Image img = Image.FromStream(fileStream);
                    fileInfo.DPI = img.HorizontalResolution.ToString();
                }
            }

            // Color depth
            fileInfo.ColorDepth = jpegDirectory.GetDescription(JpegDirectory.TagDataPrecision);

            // Compression
            fileInfo.Compression = jpegDirectory.GetDescription(JpegDirectory.TagCompressionType);

            return fileInfo;
        }

        private FileInfo FromGif(Stream fileStream)
        {
            var directories = ImageMetadataReader.ReadMetadata(fileStream);
            var fileInfo = BaseInfo(fileStream);

            GifImageDirectory gifImageDirectory = directories.OfType<GifImageDirectory>().FirstOrDefault();

            // Resolution
            int w = gifImageDirectory.GetInt32(GifImageDirectory.TagWidth);
            int h = gifImageDirectory.GetInt32(GifImageDirectory.TagHeight);
            fileInfo.Resolution = $"{w}x{h} px";

            // DPI
            Image img = Image.FromStream(fileStream);
            fileInfo.DPI = img.HorizontalResolution.ToString();

            // Color depth
            GifHeaderDirectory gifHeaderirectory = directories.OfType<GifHeaderDirectory>().FirstOrDefault();
            fileInfo.ColorDepth = gifHeaderirectory.GetDescription(GifHeaderDirectory.TagBitsPerPixel);

            // Compression
            fileInfo.Compression = "LZW";

            return fileInfo;
        }

        private FileInfo FromTiff(Stream fileStream)
        {
            var directories = ImageMetadataReader.ReadMetadata(fileStream);
            var fileInfo = BaseInfo(fileStream);

            var exifDirectory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();

            // Resolution
            int w = exifDirectory.GetInt32(ExifIfd0Directory.TagImageWidth);
            int h = exifDirectory.GetInt32(ExifIfd0Directory.TagImageHeight);
            fileInfo.Resolution = $"{w}x{h} px";

            // DPI
            fileInfo.DPI = exifDirectory.GetDescription(ExifIfd0Directory.TagXResolution);

            // Color depth
            fileInfo.ColorDepth = exifDirectory.GetDescription(ExifIfd0Directory.TagBitsPerSample);

            // Compression
            fileInfo.Compression = exifDirectory.GetDescription(ExifIfd0Directory.TagCompression);

            return fileInfo;
        }

        private FileInfo FromBmp(Stream fileStream)
        {
            var directories = ImageMetadataReader.ReadMetadata(fileStream);
            var fileInfo = BaseInfo(fileStream);

            var bmpHeaderDirectory = directories.OfType<BmpHeaderDirectory>().FirstOrDefault();

            // Resolution
            int w = bmpHeaderDirectory.GetInt32(BmpHeaderDirectory.TagImageWidth);
            int h = bmpHeaderDirectory.GetInt32(BmpHeaderDirectory.TagImageHeight);
            fileInfo.Resolution = $"{w}x{h} px";

            // DPI
            fileInfo.DPI = "72";

            // Color depth
            fileInfo.ColorDepth = bmpHeaderDirectory.GetDescription(BmpHeaderDirectory.TagBitsPerPixel);

            // Compression
            fileInfo.Compression = bmpHeaderDirectory.GetDescription(BmpHeaderDirectory.TagCompression);

            return fileInfo;
        }

        private FileInfo FromPng(Stream fileStream)
        {
            var directories = ImageMetadataReader.ReadMetadata(fileStream);
            var fileInfo = BaseInfo(fileStream);

            var pngIhdrDirectory = directories.OfType<PngDirectory>().Where(x => x.Name == "PNG-IHDR").FirstOrDefault();
            var pngPhysDirectory = directories.OfType<PngDirectory>().Where(x => x.Name == "PNG-pHYs").FirstOrDefault();

            // Resolution
            int w = pngIhdrDirectory.GetInt32(PngDirectory.TagImageWidth);
            int h = pngIhdrDirectory.GetInt32(PngDirectory.TagImageHeight);
            fileInfo.Resolution = $"{w}x{h} px";

            // DPI
            fileInfo.DPI = (pngPhysDirectory.GetInt32(PngDirectory.TagPixelsPerUnitX) / 39.37007874).ToString();

            // Color depth
            fileInfo.ColorDepth = pngIhdrDirectory.GetDescription(PngDirectory.TagBitsPerSample);

            // Compression
            fileInfo.Compression = pngIhdrDirectory.GetDescription(BmpHeaderDirectory.TagCompression);

            return fileInfo;
        }

        private FileInfo FromPcx(Stream fileStream)
        {
            var directories = ImageMetadataReader.ReadMetadata(fileStream);
            var fileInfo = BaseInfo(fileStream);

            var pcxDirectory = directories.OfType<PcxDirectory>().FirstOrDefault();

            // Resolution
            int w = pcxDirectory.GetInt32(PcxDirectory.TagXMax);
            int h = pcxDirectory.GetInt32(PcxDirectory.TagYMax);
            fileInfo.Resolution = $"{w}x{h} px";

            // DPI

            fileInfo.DPI = pcxDirectory.GetDescription(PcxDirectory.TagHorizontalDpi);

            // Color depth
            fileInfo.ColorDepth = pcxDirectory.GetDescription(PcxDirectory.TagBitsPerPixel);

            // Compression
            fileInfo.Compression = "RLE";

            return fileInfo;
        }

        private FileInfo BaseInfo(Stream fileStream)
        {
            return new FileInfo
            {
                Name = Path.GetFileName(((FileStream)fileStream).Name)
            };
        }
    }
}
