using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Ets2Map.Demo {
    public partial class Ets2MapDemo : Form {
        private enum GAME { ETS2, ATS }

        private GAME Game = GAME.ATS; // Switch to .ETS2 for ETS2

        private Ets2Mapper map;
        private MapRenderer render;

        private Ets2Point navigatePoint;
        private Ets2NavigationRoute route;

        private Timer refresh;

        private float mapScale = 10000.0f;
        
        private Point? dragPoint;
        private Ets2Point location;

        public Ets2MapDemo() {
            // Set location based on game
            switch (Game) {
                case GAME.ETS2:
                    new Ets2Point(0, 0, 0, 0);
                    break;
                case GAME.ATS:
                    new Ets2Point(-100000, 0, 17000, 0);
                    break;
            }

            // Get current folder and remove "\Ets2Map\Ets2Map.Demo\bin\[Debug|Release]"
            var projectFolder = Directory.GetCurrentDirectory();
            for (int i = 0; i < 4; i++) {
                projectFolder = projectFolder.Substring(0, projectFolder.LastIndexOf("\\"));
            }

            // Load game specific folder
            var mapFilesFolder = projectFolder + (Game == GAME.ETS2 ? "europe" : "usa");

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
            MouseMove += (s, e) => {
                if (dragPoint.HasValue) {
                    var spd = mapScale / Math.Max(this.Width, this.Height);
                    location = new Ets2Point(location.X - (e.X - dragPoint.Value.X) * spd,
                        0,
                        location.Z - (e.Y - dragPoint.Value.Y) * spd,
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


        private void Ets2MapDemo_MouseWheel(object sender, MouseEventArgs e) {
            mapScale -= e.Delta * 5;
            mapScale = Math.Max(100, Math.Min(30000, mapScale));
        }

        private void Ets2MapDemo_MouseDoubleClick(object sender, MouseEventArgs e) {
            navigatePoint = render.CalculatePointFromMap(e.X, e.Y);
        }

        private void Ets2MapDemo_Resize(object sender, EventArgs e) {
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e) {
            if (navigatePoint != null) {
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