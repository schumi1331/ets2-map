using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ets2Map.Demo
{
    public partial class Ets2MapDemo : Form
    {
        private Ets2Mapper map;
        private MapRenderer render;

        private Ets2Point navigatePoint;
        private Ets2NavigationRoute route;

        private Timer refresh;

        private float mapScale = 10000.0f;

        private Point? dragPoint;
        private Ets2Point location = new Ets2Point(0, 0, 0, 0); // ats => -100000, 0, 17000

        public Ets2MapDemo()
        {
            var projectFolder = @"D:\Projects\ets2-map\";

            var mapFilesFolder = projectFolder + "europe"; // Change this to usa for ATS map

            map = new Ets2Mapper(
                mapFilesFolder + @"\SCS\map\",
                mapFilesFolder + @"\SCS\prefab\",
                mapFilesFolder + @"\SCS\LUT\",
                mapFilesFolder + @"\LUT\");
            map.Parse(true);

            render = new MapRenderer(map, new SimpleMapPalette());

            InitializeComponent();

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            refresh = new Timer();
            refresh.Interval = 250;
            refresh.Tick += (sender, args) => Invalidate();
            refresh.Start();

            // Panning around
            MouseDown += (s, e) => dragPoint = e.Location;
            MouseUp += (s, e) => dragPoint = null;
            MouseMove += (s, e) =>
            {
                if (dragPoint.HasValue)
                {
                    var spd = mapScale/Math.Max(this.Width, this.Height);
                    location = new Ets2Point(location.X - (e.X - dragPoint.Value.X)*spd,
                        0,
                        location.Z - (e.Y - dragPoint.Value.Y)*spd,
                        0);
                    dragPoint = e.Location;
                }
            };

            // Zooming in
            MouseWheel += Ets2MapDemo_MouseWheel;

            // Navigation
            MouseDoubleClick += Ets2MapDemo_MouseDoubleClick;

            Resize += Ets2MapDemo_Resize;
        }


        private void Ets2MapDemo_MouseWheel(object sender, MouseEventArgs e)
        {
            mapScale -= e.Delta * 5;
            mapScale = Math.Max(100, Math.Min(30000, mapScale));
        }

        private void Ets2MapDemo_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            navigatePoint = render.CalculatePointFromMap(e.X, e.Y);
        }

        private void Ets2MapDemo_Resize(object sender, EventArgs e)
        {
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (navigatePoint != null)
            {
                route = map.NavigateTo(location, navigatePoint);
                navigatePoint = null;
            }
            if (route != null && route.Loading == false)
                render.SetNavigation(route);



            render.Render(e.Graphics, e.ClipRectangle, mapScale, location);

            base.OnPaint(e);
        }
    }
}