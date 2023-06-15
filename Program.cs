using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace A25;

class MyWindow : Window {
   public MyWindow () {
      Width = 800; Height = 600;
      Left = 50; Top = 50;
      WindowStyle = WindowStyle.None;
      Image image = new Image () {
         Stretch = Stretch.None,
         HorizontalAlignment = HorizontalAlignment.Left,
         VerticalAlignment = VerticalAlignment.Top,
      };
      RenderOptions.SetBitmapScalingMode (image, BitmapScalingMode.NearestNeighbor);
      RenderOptions.SetEdgeMode (image, EdgeMode.Aliased);

      mBmp = new WriteableBitmap ((int)Width, (int)Height,
         96, 96, PixelFormats.Gray8, null);
      mStride = mBmp.BackBufferStride;
      image.Source = mBmp;
      Content = image;
      MouseLeftButtonDown += OnMouseMove;

      // DrawMandelbrot (-0.5, 0, 1);
   }

   void DrawMandelbrot (double xc, double yc, double zoom) {
      try {
         mBmp.Lock ();
         mBase = mBmp.BackBuffer;
         int dx = mBmp.PixelWidth, dy = mBmp.PixelHeight;
         double step = 2.0 / dy / zoom;
         double x1 = xc - step * dx / 2, y1 = yc + step * dy / 2;
         for (int x = 0; x < dx; x++) {
            for (int y = 0; y < dy; y++) {
               Complex c = new Complex (x1 + x * step, y1 - y * step);
               SetPixel (x, y, Escape (c));
            }
         }
         mBmp.AddDirtyRect (new Int32Rect (0, 0, dx, dy));
      } finally {
         mBmp.Unlock ();
      }
   }

   byte Escape (Complex c) {
      Complex z = Complex.Zero;
      for (int i = 1; i < 32; i++) {
         if (z.NormSq > 4) return (byte)(i * 8);
         z = z * z + c;
      }
      return 0;
   }

   void OnMouseMove (object sender, MouseEventArgs e) {
      if (e.LeftButton == MouseButtonState.Pressed) {
         try {
            mBmp.Lock ();
            mBase = mBmp.BackBuffer;
            // If odd click get start point.
            if (iFirstClick) {
               var pt = e.GetPosition (this);
               int x = (int)pt.X, y = (int)pt.Y;
               SetPixel (x, y, 255);
               mBmp.AddDirtyRect (new Int32Rect (x, y, 1, 1));
               mStartPoint = pt;
            }

            // For second click draw the line
            if (!iFirstClick) {
               var ePoint = e.GetPosition (this);
               var x1 = (int)mStartPoint.X; var y1 = (int)mStartPoint.Y;
               var x2 = (int)ePoint.X; var y2 = (int)ePoint.Y;
               var xDelta = x2 - x1;
               var yDelta = y2 - y1;
               int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;

               if (xDelta < 0) dx1 = -1;
               else if (xDelta > 0) dx1 = 1;

               if (yDelta < 0) dy1 = -1;
               else if (yDelta > 0) dy1 = 1;

               if (xDelta < 0) dx2 = -1;
               else if (xDelta > 0) dx2 = 1;

               int xDiff = Math.Abs (xDelta);
               int yDiff = Math.Abs (yDelta);
               if (!(xDiff > yDiff)) {
                  xDiff = Math.Abs (yDelta);
                  yDiff = Math.Abs (xDelta);
                  if (yDelta < 0) dy2 = -1; else if (yDelta > 0) dy2 = 1;
                  dx2 = 0;
               }
               int n = xDiff / 2;
               // Run through the loop to set the pixel.
               for (int i = 0; i <= xDiff; i++) {
                  SetPixel (x1, y1, 255);
                  mBmp.AddDirtyRect (new Int32Rect (x1, y1, 1, 1));
                  n += yDiff;
                  if (!(n < xDiff)) {
                     n -= xDiff;
                     x1 += dx1;
                     y1 += dy1;
                  } else {
                     x1 += dx2;
                     y1 += dy2;
                  }
               }
            }

            if (iFirstClick) iFirstClick = false;
            else iFirstClick = true;
         } finally {
            mBmp.Unlock ();
         }
      }
   }

   bool iFirstClick = true;
   System.Windows.Point mStartPoint;

   void DrawGraySquare () {
      try {
         mBmp.Lock ();
         mBase = mBmp.BackBuffer;
         for (int x = 0; x <= 255; x++) {
            for (int y = 0; y <= 255; y++) {
               SetPixel (x, y, (byte)x);
            }
         }
         mBmp.AddDirtyRect (new Int32Rect (0, 0, 256, 256));
      } finally {
         mBmp.Unlock ();
      }
   }

   void SetPixel (int x, int y, byte gray) {
      unsafe {
         var ptr = (byte*)(mBase + y * mStride + x);
         *ptr = gray;
      }
   }

   WriteableBitmap mBmp;
   int mStride;
   nint mBase;
}

internal class Program {
   [STAThread]
   static void Main (string[] args) {
      Window w = new MyWindow ();
      w.Show ();
      Application app = new Application ();
      app.Run ();
   }
}
