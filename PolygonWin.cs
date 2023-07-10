using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GrayBMP {
   class PolyFillWin : Window {

      public PolyFillWin () {
         Width = 900; Height = 650;
         Left = 200; Top = 50;
         WindowStyle = WindowStyle.None;
         mBmp = new GrayBMP (Width, Height);
         p2 = new PolyFill (mBmp);
         Image image = new () {
            Stretch = Stretch.None,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Source = mBmp.Bitmap
         };
         RenderOptions.SetBitmapScalingMode (image, BitmapScalingMode.NearestNeighbor);
         RenderOptions.SetEdgeMode (image, EdgeMode.Aliased);

         Loaded += OnWindowLoaded;
         Content = image;
      }

      private void OnWindowLoaded (object sender, RoutedEventArgs e) {
         // Read all line from file.
         // Assuming all lines have exactly start and end points points.
         var lines = File.ReadAllLines (@"C:\Temp\leaf-fill2.txt");
         foreach (var line in lines) {
            var points = line.Split (' ').Select (int.Parse).ToList ();
            p2.AddLine (points[0], points[1], points[2], points[3]);
         }
         p2.Fill (mBmp, 255);
      }

      PolyFill p2;
      readonly GrayBMP mBmp;
   }

   class PolyFill {

      public PolyFill (GrayBMP bmp) {
         mBmp = bmp;
         mPoints = new List<Point> ();
         mLines = new List<(Point S, Point E)> ();
         YEnd = mBmp.Height;
      }

      /// <summary>Add line to bitmap</summary>
      public void AddLine (int x0, int y0, int x1, int y1) {
         var startPoint = new Point (x0, y0);
         var endPoint = new Point (x1, y1);
         mPoints.Add (startPoint); mPoints.Add (endPoint);
         mLines.Add ((startPoint, endPoint));
         AddLine2 (x0, y0, x1, y1);
      }

      void AddLine2 (int x0, int y0, int x1, int y1) {
         mBmp.Begin ();
         mBmp.DrawLine (x0, y0, x1, y1, 255);
         mBmp.End ();
      }

      public void Fill (GrayBMP bmp, int color) {
         // Logics.
         // Sort Ymin to Sort Ymax.
         mPoints = mPoints.OrderBy (x => x.Y).ToList ();
         // Finding Global Edge Table.
         List<List<Node>> GET = new ();
         for (int y = YStart; y <= YEnd; y++) {
            GET.Add (null);
            // Sort points with Y increasing order.
            var YPoints = mPoints.Where (p => p.Y == y).Distinct ().ToList ();
            if (YPoints.Count > 0) {
               List<Node> connectedPoints = new ();
               foreach (var pt in YPoints) {
                  // Get the connections to YP.
                  // Find the edges connected to the current point.
                  var YPConnections = mLines.Where (x => ((x.S.X == pt.X && x.S.Y == pt.Y) || (x.E.X == pt.X && x.E.Y == pt.Y))).ToList ();
                  List<Point> cPoints = new ();
                  // First Line point.
                  var p1 = pt;
                  foreach (var (S, E) in YPConnections) {
                     // Second Line point
                     Point p2;
                     if (p1.X == S.X && p1.Y == S.Y) p2 = E;
                     else p2 = S;

                     if (!mSolvedEdges.Contains (p2)) cPoints.Add (p2);
                  }
                  cPoints = cPoints.OrderBy (x => x.Y).ToList ();

                  // Y intersection point connections in the plane.
                  foreach (var p2 in cPoints) {
                     var yMax = Math.Max (p1.Y, p2.Y);
                     var xOfYMin = new List<Point> () { p1, p2 }.OrderBy (x => x.Y).First ().X;
                     double invSlope = 0.0;

                     var num = p2.Y - p1.Y;
                     var denom = p2.X - p1.X;
                     if (denom != 0) {
                        var s = (double)num / denom;
                        invSlope = (double)(1 / s);
                        //invSlope = Math.Round (invSlope, 5);
                     }
                     connectedPoints.Add (new Node { Ymax = yMax, XofYMin = xOfYMin, Slope = invSlope });
                  }
                  if (cPoints.Count > 0) mSolvedEdges.Add (p1);
               }
               GET[y] = connectedPoints;
            }
         }

         List<Node> PrevNodes = new ();
         // Finding Active Edge Table.
         // TODO: Set YEnd: to 570 example to output leaf-fill.txt
         // Issue yet to be determined.
         for (int y = YStart; y <= YEnd; y++) {
            var nodes = GET[y];
            if (nodes != null) {
               nodes.AddRange (PrevNodes);
               PrevNodes = nodes;
               // When nodes are multiple of 3, we cannot form pair.
               // Reduce the node to 2.
               if (PrevNodes.Count % 3 == 0) {
                  PrevNodes = PrevNodes.Where (c => c.Ymax != y).ToList ();
               }
               var x0 = PrevNodes[0].XofYMin;
               var y0 = y;
               var x1 = PrevNodes[1].XofYMin;
               var y1 = y;

               // Apply the computed pair.
               AddLine2 ((int)x0, y0, (int)x1, y1);
            } else {
               if (PrevNodes.Count >= 2) {
                  var first = PrevNodes[0];
                  var last = PrevNodes[1];
                  if (first != null && last != null) {
                     first.XofYMin += first.Slope;
                     last.XofYMin += last.Slope;
                     // When reached end of polygon.
                     if (first.Ymax == last.Ymax && last.XofYMin == first.XofYMin) {
                        return;
                     }
                     AddLine ((int)first.XofYMin, y, (int)last.XofYMin, y);
                  }
               }
            }
         }
      }

      /// <summary>Struct Point</summary>
      public struct Point {
         public Point (int x, int y) {
            X = x;
            Y = y;
         }
         public int X { get; set; }
         public int Y { get; set; }
      }

      /// <summary>Class Node</summary>
      class Node {
         public int Ymax { get; set; }
         public double XofYMin { get; set; }
         public double Slope { get; set; }
      }

      int YStart;
      int YEnd;
      public List<Point> mPoints;
      public List<Point> mSolvedEdges = new ();
      public List<(Point S, Point E)> mLines;
      public GrayBMP mBmp;
   }
}
