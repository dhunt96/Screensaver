using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices; // DllImport
using Microsoft.Win32; // RegistryKey

// This code is taken from the tutorial found at
// http://www.harding.edu/fmccown/screensaver/screensaver.html

namespace ScreenSaver
{
    public partial class ScreenSaverForm : Form
    {
        class Vector
        {
            float x;
            float y;
            public Vector(float X, float Y)
            {
                x = X;
                y = Y;
            }
            public float X
            {
                get { return x; }
                set { x = value; }
            }
            public float Y
            {
                get { return y; }
                set { y = value; }
            }
            public float Magnitude
            {
                get
                {
                    return (float)Math.Sqrt(x * x + y * y);
                }
            }
            public Vector Normalize()
            {
                return (1.0f / this.Magnitude) * this;
            }
            public static Vector operator +(Vector c1, Vector c2) // Vector addition
            {
                return new Vector(c1.X + c2.X, c1.Y + c2.Y);
            }
            public static Vector operator -(Vector c1, Vector c2) // Vector subtraction
            {
                return new Vector(c1.X - c2.X, c1.Y - c2.Y);
            }
            public static float operator *(Vector c1, Vector c2) // Dot product
            {
                return c1.X * c2.X + c1.Y * c2.Y;
            }
            public static Vector operator *(float s, Vector v) // Scalar multiplication (scalar is prefix)
            {
                return new Vector(s * v.X, s * v.Y);
            }
            public static Vector operator *(Vector v, float s) // Scalar multiplication (scalar is suffix)
            {
                return new Vector(s * v.X, s * v.Y);
            }
        }

        class Atom
        {
            Vector position; // center of atom
            Vector velocity; // pixels per tick
            float radius; // collision radius in pixels
            Bitmap image; // render once makes more efficent
            public Atom(Vector Position, Vector Velocity, float Radius, Color Color, float Shininess)
            {
                position = Position;
                velocity = Velocity;
                radius = Radius;
                int OriginalPixelRadius = Convert.ToInt32(3 * Radius);
                while ((2 * OriginalPixelRadius + 1) % 3 != 0)
                    ++OriginalPixelRadius;
                // maybe compute OriginalPixelRadius here (so make Sphere dimensions a multiple of 3)
                image = Shrink(Sphere(Color, OriginalPixelRadius, Shininess));
            }
            public Vector Position
            {
                get { return position; }
            }
            public Vector Velocity
            {
                get { return velocity; }
                set { velocity = value; }
            }
            public float Radius
            {
                get { return radius; }
            }
            public Bitmap Image
            {
                get { return image; }
            }
            public float Distance(Atom A)
            {
                return (float)Math.Sqrt((position.X - A.Position.X) * (position.X - A.Position.X) + (position.Y - A.Position.Y) * (position.Y - A.Position.Y));
            }
            public void Move(int MaxX, int MaxY)
            {
                position += velocity;
                // Ricochet off walls
                if ((position.X - radius <= 0) && (velocity.X <= 0)) velocity.X = -velocity.X;
                if ((position.X + radius >= MaxX) && (velocity.X >= 0)) velocity.X = -velocity.X;
                if ((position.Y - radius <= 0) && (velocity.Y <= 0)) velocity.Y = -velocity.Y;
                if ((position.Y + radius >= MaxY) && (velocity.Y >= 0)) velocity.Y = -velocity.Y;
            }
            private Color Shade(Color C, float Intensity)
            {
                int Red = Convert.ToInt32(C.R * Intensity);
                int Green = Convert.ToInt32(C.G * Intensity);
                int Blue = Convert.ToInt32(C.B * Intensity);
                return Color.FromArgb(Red, Green, Blue);
            }
            private Bitmap Sphere(Color C, float R, float Shininess)
            {
                int r = Convert.ToInt32(Math.Ceiling(R));
                Bitmap B = new Bitmap(2 * r + 1, 2 * r + 1);
                for (int x = -r; x <= r; x++)
                    for (int y = -r; y <= r; y++)
                    {
                        float d = (float)Math.Sqrt((x * x) + (y * y));
                        if (d <= R) // pixel is inside sphere
                        {
                            float Intensity = (float)Math.Pow(Math.Cos(d / R), Shininess);
                            B.SetPixel(x + r, y + r, Shade(C, Intensity));
                        }
                        else
                            B.SetPixel(x + r, y + r, Color.Black); // otherwise pixels are transparent
                        // Consider passing background color as parameter
                    }
                return B;
            }
            private Bitmap Shrink(Bitmap B) // automatic factor of 3 (assumes dimensions are even multiple of 3)
            {
                int w = B.Width / 3;
                int h = B.Height / 3;
                Bitmap Result = new Bitmap(w, h);
                for (int x = 0; x < w; x++)
                    for (int y = 0; y < h; y++)
                    {
                        int Red = 0;
                        int Green = 0;
                        int Blue = 0;
                        for (int i = 3 * x; i < 3 * x + 3; i++)
                            for (int j = 3 * y; j < 3 * y + 3; j++)
                            {
                                Red += B.GetPixel(i, j).R;
                                Green += B.GetPixel(i, j).G;
                                Blue += B.GetPixel(i, j).B;
                            }
                        Red = Convert.ToInt32(Red / 9.0f);
                        Green = Convert.ToInt32(Green / 9.0f);
                        Blue = Convert.ToInt32(Blue / 9.0f);
                        if (Red > 255) Red = 255;
                        if (Green > 255) Green = 255;
                        if (Blue > 255) Blue = 255;
                        Result.SetPixel(x, y, Color.FromArgb(Red, Green, Blue));
                    }
                Result.MakeTransparent(Result.GetPixel(0, 0));
                return Result;
            }
        }

        System.Collections.ArrayList Atoms;

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out Rectangle lpRect);

        private Point mouseLocation;
        private Random rand = new Random();
        private bool previewMode = false;
        Bitmap Space;
        int AtomCount;

        public ScreenSaverForm(Rectangle Bounds)
        {
            InitializeComponent();
            this.Bounds = Bounds;
            InitAtoms(1.0F);
        }

        public ScreenSaverForm(IntPtr PreviewWndHandle)
        {
            InitializeComponent();
            // Set the preview window as the parent of this window
            SetParent(this.Handle, PreviewWndHandle);
            // Make this a child window so it will close when the parent dialog closes
            // GWL_STYLE = -16, WS_CHILD = 0x40000000
            SetWindowLong(this.Handle, -16, new IntPtr(GetWindowLong(this.Handle, -16) | 0x40000000));
            // Place our window inside the parent
            Rectangle ParentRect;
            GetClientRect(PreviewWndHandle, out ParentRect);
            Size = ParentRect.Size;
            // Make atoms smaller
            previewMode = true;
            InitAtoms(0.10F);
        }

        private void InitAtoms(float Scale)
        {
            // Use the atoms field from the Registry - if it exists
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Atoms_ScreenSaver");
            if (key == null)
                AtomCount = 10;
            else
                AtomCount = Convert.ToInt32(key.GetValue("atoms"));
            // Create Atoms
            int MaxX = this.Width;
            int MaxY = this.Height;
            Atoms = new System.Collections.ArrayList();
            Random R = new Random();
            int Placed = 0;
            while (Placed < AtomCount)
            {
                // Generate new potential position
                int x = R.Next(MaxX-50) + 25;
                int y = R.Next(MaxY-50) + 25;
                bool Ok = true;
                // Search current atoms for overlap
                foreach (Atom a in Atoms)
                {
                    double d = Math.Sqrt((a.Position.X - x) * (a.Position.X - x) + (a.Position.Y - y) * (a.Position.Y - y));
                    if (d < 2 * a.Radius)
                    {
                        Ok = false;
                        break;
                    }
                }
                // if no Overlap with current atoms, then add to Atoms
                if (Ok)
                {
                    Atoms.Add(new Atom(new Vector(x, y), new Vector(Scale * (R.Next(20)-10), Scale * (R.Next(20)-10)), Scale * 25, Color.Red, 1.75f));
                    Placed++;
                }
            }
        }

        private void ScreenSaverForm_Load(object sender, EventArgs e)
        {
            Space = new Bitmap(this.Width, this.Height);
            Cursor.Hide();
            TopMost = true;
            moveTimer.Interval = 25;
            moveTimer.Tick += new EventHandler(moveTimer_Tick);
            moveTimer.Start();
        }

        private void ScreenSaverForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (!previewMode)
            {
                if (!mouseLocation.IsEmpty)
                {
                    // Terminate if mouse is moved a "significant" distance
                    if ((Math.Abs(mouseLocation.X - e.X) > 5) || (Math.Abs(mouseLocation.Y - e.Y) > 5))
                        Application.Exit();
                }
                // Update current mouse location
                mouseLocation = e.Location;
            }
        }

        private void ScreenSaverForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (!previewMode)
                Application.Exit();
        }

        private void ScreenSaverForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!previewMode)
                Application.Exit();
        }

        private void moveTimer_Tick(object sender, System.EventArgs e)
        {
            Graphics g = Graphics.FromImage(Space);
            SolidBrush B = new SolidBrush(Color.Black);
            g.FillRectangle(B, 0, 0, Space.Width, Space.Height);

            // move each atom (detecting collisions and updating velocities)
            foreach (Atom a in Atoms)
            {
                a.Move(this.Width, this.Height);
                // check for collisions (overlap and heading towards other atom)
                foreach (Atom b in Atoms)
                    if (a != b) // don't compare atom with itself
                    {
                        if (a.Distance(b) < (a.Radius + b.Radius)) // within collision distance
                        {
                            Vector towards = b.Position - a.Position; // vector pointing from a towards b
                            if (Math.Acos((a.Velocity * towards) / (a.Velocity.Magnitude * towards.Magnitude)) <= Math.PI / 2.0)
                            {
                                // Collision has occured
                                Vector relative = a.Velocity - b.Velocity; // relative impact if b atom stationary
                                towards = towards.Normalize(); // only normalize if collision occured - more efficient
                                Vector component = (relative * towards) * towards; // component of relative along towards
                                b.Velocity += component;
                                a.Velocity += -1 * component; // equal and opposite reaction
                            }
                        }
                    }
                g.DrawImage(a.Image, a.Position.X - a.Image.Width / 2, a.Position.Y - a.Image.Height / 2);
                // Note: Need Convert to eliminate first row/column missing when drawing image
            }

            Graphics FormG = this.CreateGraphics();
            FormG.DrawImage(Space, 0, 0);
        }

    }
}
