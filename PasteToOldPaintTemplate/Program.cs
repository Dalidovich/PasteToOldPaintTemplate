using System.Drawing;

namespace PasteToOldPaintTemplate
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1 || !Path.Exists(args[0]) || !File.Exists(args[0]))
            {
                Console.WriteLine("Path to file not found");
                Console.ReadLine();
                return;
            }

            var path = args[0];

            Console.WriteLine("Input pixel size:");
            if (!int.TryParse(Console.ReadLine(), out int pixelSize) || pixelSize <= 0)
            {
                Console.WriteLine("Invalid input pixel size");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Input number of color palettes(default is 28):");
            if (!int.TryParse(Console.ReadLine(), out int countOfColor) || countOfColor <= 1 || countOfColor > 28)
            {
                Console.WriteLine("Invalid input number of color palettes");
                Console.ReadLine();
                return;
            }

            var bitmap = new Bitmap(path);
            bitmap = CreatePixelImg(bitmap, pixelSize, countOfColor);

            var pixels = GetPixelsFromBitmap(bitmap);
            var largePixelColor = GetLargePixelColor(pixels);
            var pallete = pixels.ToHashSet().ToList();

            bitmap = ResizeImageWithBackground(bitmap, TemplateConst.MainAreaWidth, TemplateConst.MainAreaHeight,
                Color.FromArgb(255, largePixelColor.R, largePixelColor.G, largePixelColor.B));

            var a = new MemoryStream(Convert.FromBase64String(TemplateConst.Template));
            var bitmapWithTemplate = PasteImgInTemplate(bitmap, new Bitmap(a), TemplateConst.MainAreaX, TemplateConst.MainAreaY);
            bitmapWithTemplate = PastePallete(bitmapWithTemplate, pallete, largePixelColor);
            bitmapWithTemplate = PasteTitle(bitmapWithTemplate, path);
            var savePath = $"{path.Substring(0, path.LastIndexOf('.'))}_PintPixel_ps{pixelSize}_cc{countOfColor}{path.Substring(path.LastIndexOf('.'))}";
            bitmapWithTemplate.Save(savePath);
        }

        public static Bitmap PasteTitle(Bitmap withTemplate, string path)
        {
            var fileName = Path.GetFileName(path);
            if (fileName.Length > TemplateConst.CountOfTrimCharOnTitle)
                fileName = $"{fileName.Substring(0, TemplateConst.CountOfTrimCharOnTitle)}...";
            var title = $"{fileName}  -  Paint";
            var block = new Bitmap(TemplateConst.TitlePaintBlockSizeX, TemplateConst.TitlePaintBlockSizeY);
            for (int h = 0; h < block.Height; h++)
                for (int w = 0; w < block.Width; w++)
                    block.SetPixel(w, h, Color.FromArgb(255, TemplateConst.TitleBlockColor.R, TemplateConst.TitleBlockColor.G, TemplateConst.TitleBlockColor.B));


            using (var graphics = Graphics.FromImage(withTemplate))
            {
                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;


                var blockX = TemplateConst.TitlePaintBlockX;
                var blockY = TemplateConst.TitlePaintBlockY;
                graphics.DrawImage(block, blockX, blockY);


                var x = TemplateConst.TitleX;
                var y = TemplateConst.TitleY;

                using (var font = new Font("Fixedsys", 11, FontStyle.Bold, GraphicsUnit.World))
                using (var brush = new SolidBrush(Color.White))
                using (var format = new StringFormat())
                {
                    format.FormatFlags = StringFormatFlags.NoClip;
                    format.Trimming = StringTrimming.None;
                    DrawTextWithSpacing(graphics, title, font, brush, x, y, 0.7f);
                }
            }

            return withTemplate;
        }

        private static void DrawTextWithSpacing(Graphics graphics, string text, Font font, Brush brush, float x, float y, float spacingFactor)
        {
            for (int i = 0; i < text.Length; i++)
            {
                string character = text[i].ToString();
                graphics.DrawString(character, font, brush, x, y);
                SizeF charSize = graphics.MeasureString(character, font);
                x += charSize.Width * spacingFactor;
            }
        }

        public static Bitmap PastePallete(Bitmap bitmap, List<Pixel> pallete, Pixel largePixelColor)
        {
            var countOfMissingColor = TemplateConst.CountOfPallete - pallete.Count;
            for (int i = 0; i < countOfMissingColor; i++)
            {
                pallete.Add(new Pixel() { R = byte.MaxValue, G = byte.MaxValue, B = byte.MaxValue });
            }

            for (int i = 0; i < pallete.Count; i++)
            {
                var onePallete = new Bitmap(TemplateConst.PalleteSize, TemplateConst.PalleteSize);
                for (int h = 0; h < onePallete.Height; h++)
                    for (int w = 0; w < onePallete.Width; w++)
                        onePallete.SetPixel(w, h, Color.FromArgb(255, pallete[i].R, pallete[i].G, pallete[i].B));

                using (var graphics = Graphics.FromImage(bitmap))
                {
                    var x = TemplateConst.PalleteX + (i % (TemplateConst.CountOfPallete / 2) * TemplateConst.PalleteSize) + (i % (TemplateConst.CountOfPallete / 2) * TemplateConst.PixelBetweenPallete);
                    var y = TemplateConst.PalleteY + (i >= (TemplateConst.CountOfPallete / 2) ? TemplateConst.PalleteSize + TemplateConst.PixelBetweenPallete : 0);

                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    graphics.DrawImage(onePallete, x, y);
                }
            }

            var currentPallete = new Bitmap(TemplateConst.CurrentPalleteSize, TemplateConst.CurrentPalleteSize);
            for (int h = 0; h < currentPallete.Height; h++)
                for (int w = 0; w < currentPallete.Width; w++)
                    currentPallete.SetPixel(w, h, Color.FromArgb(255, largePixelColor.R, largePixelColor.G, largePixelColor.B));

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                graphics.DrawImage(currentPallete, TemplateConst.CurrentPalleteX, TemplateConst.CurrentPalleteY);
            }

            return bitmap;
        }

        public static List<Pixel> GetPixelsFromBitmap(Bitmap bitmap)
        {
            var pixels = new List<Pixel>();

            for (int y = 0; y < bitmap.Height; y += 1)
            {
                for (int x = 0; x < bitmap.Width; x += 1)
                {
                    pixels.Add(new Pixel()
                    {
                        R = bitmap.GetPixel(x, y).R,
                        G = bitmap.GetPixel(x, y).G,
                        B = bitmap.GetPixel(x, y).B,
                    });
                }
            }

            return pixels;
        }

        public static Pixel GetLargePixelColor(List<Pixel> pixels)
        {
            return pixels.GroupBy(p => new Pixel() { R = p.R, G = p.G, B = p.B })
                                    .OrderByDescending(g => g.Count())
                                    .First().Key;
        }

        public static Bitmap CreatePixelImg(Bitmap bitmap, int pixelSize, int countOfColor)
        {
            var newWidth = (int)Math.Ceiling((double)bitmap.Width / pixelSize);
            var newHeight = (int)Math.Ceiling((double)bitmap.Height / pixelSize);

            var pixels = new List<Pixel>();

            for (int y = 0; y < bitmap.Height; y += pixelSize)
            {
                for (int x = 0; x < bitmap.Width; x += pixelSize)
                {
                    int blockWidth = Math.Min(pixelSize, bitmap.Width - x);
                    int blockHeight = Math.Min(pixelSize, bitmap.Height - y);

                    pixels.Add(GetAvgRGBCanal(bitmap, x, y, blockWidth, blockHeight));
                }
            }
            pixels = ReduceColorsKMeans(pixels.ToArray(), countOfColor).ToList();
            var largePixelColor = GetLargePixelColor(pixels);
            var newBitmap = new Bitmap(newWidth, newHeight);

            for (int i = 0; i < pixels.Count; i++)
            {
                int x = i % newWidth;
                int y = i / newWidth;

                if (y < newHeight)
                {
                    var color = Color.FromArgb(255, pixels[i].R, pixels[i].G, pixels[i].B);
                    newBitmap.SetPixel(x, y, color);
                }
            }
            return newBitmap;
        }

        public static Bitmap PasteImgInTemplate(Image originalImage, Image TemplateImage, int pasteX, int pasteY)
        {
            var newBitmap = new Bitmap(TemplateImage);

            using (var graphics = Graphics.FromImage(newBitmap))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                graphics.DrawImage(originalImage, pasteX, pasteY);
            }

            return newBitmap;
        }

        public static Bitmap ResizeImageWithBackground(Image originalImage, int targetWidth, int targetHeight, Color backgroundColor)
        {
            double ratioX = (double)targetWidth / originalImage.Width;
            double ratioY = (double)targetHeight / originalImage.Height;
            double ratio = Math.Min(ratioX, ratioY);

            int newWidth = (int)(originalImage.Width * ratio);
            int newHeight = (int)(originalImage.Height * ratio);

            var newBitmap = new Bitmap(targetWidth, targetHeight);

            using (var graphics = Graphics.FromImage(newBitmap))
            {
                graphics.Clear(backgroundColor);
                int x = (targetWidth - newWidth) / 2;
                int y = (targetHeight - newHeight) / 2;

                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                graphics.DrawImage(originalImage, x, y, newWidth, newHeight);
            }

            return newBitmap;
        }

        public static Pixel[] ReduceColorsKMeans(Pixel[] pixels, int targetColors)
        {
            if (pixels.Length == 0 || targetColors <= 0) return pixels;
            var points = pixels.Select(p => new double[] { p.R, p.G, p.B }).ToArray();
            var random = new Random();
            var centroids = new List<double[]>();
            for (int i = 0; i < targetColors; i++)
            {
                var randomPixel = points[random.Next(points.Length)];
                centroids.Add(new double[] { randomPixel[0], randomPixel[1], randomPixel[2] });
            }
            for (int iteration = 0; iteration < 10; iteration++)
            {
                var clusters = new List<int>[targetColors];
                for (int i = 0; i < targetColors; i++)
                    clusters[i] = new List<int>();

                for (int i = 0; i < points.Length; i++)
                {
                    int closestCentroid = 0;
                    double minDistance = double.MaxValue;

                    for (int j = 0; j < centroids.Count; j++)
                    {
                        double distance = Math.Sqrt(
                            Math.Pow(points[i][0] - centroids[j][0], 2) +
                            Math.Pow(points[i][1] - centroids[j][1], 2) +
                            Math.Pow(points[i][2] - centroids[j][2], 2));

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestCentroid = j;
                        }
                    }
                    clusters[closestCentroid].Add(i);
                }

                for (int i = 0; i < targetColors; i++)
                {
                    if (clusters[i].Count > 0)
                    {
                        double avgR = clusters[i].Average(idx => points[idx][0]);
                        double avgG = clusters[i].Average(idx => points[idx][1]);
                        double avgB = clusters[i].Average(idx => points[idx][2]);

                        centroids[i] = new double[] { avgR, avgG, avgB };
                    }
                }
            }

            Pixel[] palette = new Pixel[centroids.Count];
            for (int i = 0; i < centroids.Count; i++)
            {
                palette[i] = new Pixel
                {
                    R = (byte)Math.Round(centroids[i][0]),
                    G = (byte)Math.Round(centroids[i][1]),
                    B = (byte)Math.Round(centroids[i][2])
                };
            }

            Pixel[] result = new Pixel[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                int closestColorIndex = 0;
                double minDistance = double.MaxValue;

                for (int j = 0; j < palette.Length; j++)
                {
                    double distance = Math.Sqrt(
                        Math.Pow(pixels[i].R - palette[j].R, 2) +
                        Math.Pow(pixels[i].G - palette[j].G, 2) +
                        Math.Pow(pixels[i].B - palette[j].B, 2));

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestColorIndex = j;
                    }
                }

                result[i] = palette[closestColorIndex];
            }

            return result;
        }

        public static Pixel GetAvgRGBCanal(Bitmap bitmap, int x, int y, int blockWidth, int blockHeight)
        {
            int[] rgb = new int[3] { 0, 0, 0 };
            int pixelCount = 0;

            for (int i = 0; i < blockHeight; i++)
            {
                for (int k = 0; k < blockWidth; k++)
                {
                    var pixel = bitmap.GetPixel(x + k, y + i);
                    rgb[0] += pixel.R;
                    rgb[1] += pixel.G;
                    rgb[2] += pixel.B;
                    pixelCount++;
                }
            }
            return new Pixel
            {
                R = (byte)(rgb[0] / pixelCount),
                G = (byte)(rgb[1] / pixelCount),
                B = (byte)(rgb[2] / pixelCount),
            };
        }
    }
}
