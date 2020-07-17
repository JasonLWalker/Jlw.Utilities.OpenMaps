using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jlw.Utilities.OpenMaps
{
    public class ImageInfo
    {
        public byte[] ImageBytes
        {
            get
            {
                if(ImageData.TryGetSinglePixelSpan(out var pixelSpan))
                {
                     return MemoryMarshal.AsBytes(pixelSpan).ToArray();
                }
                return new byte[]{};
            }
        }

        //MemoryMarshal.AsBytes(ImageData.GetPixelSpan()).ToArray();
        public Image<Rgba32> ImageData { get; protected set; }
        public string Source { get; protected set; }
        public HttpStatusCode StatusCode { get;  protected set;}
        public string Status { get; protected set; }

        public ImageInfo(string source, HttpStatusCode statusCode = HttpStatusCode.Ambiguous, string status="", byte[] imageData = null)
        {
            Source = source;
            Status = status;
            StatusCode = statusCode;
            if (imageData != null)
                SetImageData(imageData);
        }

        public void SetImageData(byte[] data)
        {
            ImageData = Image.Load(data);
        } 

        public void SetImageData(Image<Rgba32> data)
        {
            ImageData = data.Clone();
        }

        public static Image<Rgba32> GetErrorTile(int x, int y, int zoom)
        {
            Image<Rgba32> img = new Image<Rgba32>(Configuration.Default, 256, 256);
            img.Mutate(ctx=>
            {
                ctx.BackgroundColor(Color.White);
                ctx.DrawText($"Unable to retrieve Tile {x}, {y}, {zoom}", SystemFonts.CreateFont("Arial", 10, FontStyle.Regular), Color.Black, new PointF(60, 128));
                
            });
            return img;
        }

        public byte[] GetAsPng()
        {
            byte[] result = null;
            if (ImageData != null)
            {
                using (MemoryStream outputStream = new MemoryStream())
                {
                    ImageData.Save(outputStream, new PngEncoder());
                    result = outputStream.ToArray();
                }
            }

            return result ?? new byte[]{};
        }
    }
}