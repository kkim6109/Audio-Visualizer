using System;
using System.Drawing;
using System.Windows.Forms;

namespace VectorVisualizer
{
    public partial class Form1 : Form
    {
        Graphics graphics;
        SolidBrush emptySquare;
        SolidBrush colored;
        System.IO.Stream music = Properties.Resources.song;
        System.IO.Stream music2 = Properties.Resources.song; // Lazy way to read bytes
        System.Media.SoundPlayer background;

        private GridPoint[,] grid;
        public static Rainbow rainbow;
        private static int PIXELS = 511; // Size of the display area
        int divisor;
        int totalFrames;
        int curFrame = -1;
        public int amtShorts;
        DateTime start;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            background = new System.Media.SoundPlayer(music);
            int maxAmt = -65536;
            divisor = maxAmt / PIXELS;
            byte[] bytes = new byte[music2.Length];
            music2.Read(bytes, 0, bytes.Length);
            int byterate = BitConverter.ToInt32(bytes, 28); // Bytes per second
            long songLength = (long)((double)bytes.Length / byterate * (double)1000); // 1000 milliseconds per second
            totalFrames = (int)(songLength / timer1.Interval);

            convertAudioToShort(bytes);

            rainbow = new Rainbow();
            rainbow.size = 5f;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (curFrame == -1)
            {
                start = DateTime.Now;
                background.Play();
            }
            try
            {
                curFrame = (int)((DateTime.Now.Ticks - start.Ticks) / 10000 / timer1.Interval);
                //Console.WriteLine(((DateTime.Now.Ticks - start.Ticks) / 10000 / timer1.Interval) + " " + totalFrames);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                return;
            }
            if (curFrame >= totalFrames)
            {
                timer1.Stop();
                return;
            }

            graphics = this.CreateGraphics();
            Bitmap BackBuffer = new Bitmap(PIXELS, PIXELS);
            Graphics tempGraphics = Graphics.FromImage(BackBuffer);
            emptySquare = new SolidBrush(Color.Black);
            colored = new SolidBrush(rainbow.nextColor());
            tempGraphics.FillRectangle(emptySquare, new Rectangle(0, 0, PIXELS, PIXELS));
            for (int i = 0; i < amtShorts; i++) // width
            {
                GridPoint point = grid[curFrame, i];
                tempGraphics.FillEllipse(colored, new Rectangle(point.x - 1, point.y - 1, 3, 3));
            }

            graphics.TranslateTransform(PIXELS / 1.416f, 0);
            graphics.RotateTransform(45);

            graphics.DrawImageUnscaled(BackBuffer, 0, 0);
            tempGraphics.Dispose();
            BackBuffer.Dispose();
        }
        
        public void convertAudioToShort(byte[] bytes)
        {
            short[] shorts = new short[0];
            try
            {
                int channels = bytes[22];
                int pos = 12;

                // Keep iterating until we find the data chunk
                while (!(bytes[pos] == 100 && bytes[pos + 1] == 97 && bytes[pos + 2] == 116 && bytes[pos + 3] == 97))
                {
                    pos += 4;
                    int chunkSize = bytes[pos] + bytes[pos + 1] * 256 + bytes[pos + 2] * 65536 + bytes[pos + 3] * 16777216;
                    pos += 4 + chunkSize;
                }
                pos += 8;

                shorts = new short[(bytes.Length - pos) / 2];
                for (int i = 0; i < shorts.Length; i++)
                {
                    shorts[i] = BitConverter.ToInt16(bytes, pos);
                    pos += 2;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            amtShorts = shorts.Length / 2 / totalFrames;

            grid = new GridPoint[totalFrames, amtShorts];

            for (int i = 0; i < totalFrames; i++)
            {
                int location = i * amtShorts * 2;
                for (int j = 0; j < amtShorts; j++)
                {
                    int x = shorts[location + j * 2] / divisor + PIXELS / 2;
                    int y = shorts[location + j * 2 + 1] / divisor + PIXELS / 2;
                    grid[i, j] = new GridPoint(x, y);
                }
            }
        }
    }


    public class GridPoint
    {
        public int x;
        public int y;

        public GridPoint(int locX, int locY)
        {
            x = locX;
            y = locY;
        }
    }

    public class Rainbow
    {
        private double r, g, b;
        private int step;
        public double size;
        private bool positive = true;

        //--------------------------------------------------------------
        //sets up the rainbow
        //--------------------------------------------------------------
        public Rainbow()
        {
            size = 1; // default size, the size can be between 0 non-inclusive and 255 inclusive (Or 0.0-1.0 if working with another Color standard)
            g = b = 0;
            r = 255; // Starts color as red, change to 1.0 if working with another color standard
            step = 0;
        }

        //--------------------------------------------------------------
        //Updates the given color variable to the next iteration
        //--------------------------------------------------------------
        private double updateColor(double col)
        {
            if (positive)
            {
                col += size; // increases the color if it started at 0
            }
            else
            {
                col -= size; // decreases the color if it started at 255
            }

            if (col <= 0 || col >= 255)
            { // Why does java Math not have a clamp method
                col = col < 0 ? 0 : col;
                col = col > 255 ? 255 : col;
                step++; // Goes to the next step in the rainbow fade
                positive = !positive; // since each step adds or subtracts from the variable
                if (step >= 3) // loops the steps
                {
                    step = 0;
                }
            }

            return col;
        }

        //--------------------------------------------------------------
        //updates the rainbow
        //--------------------------------------------------------------
        public Color nextColor()
        {
            // Switch case can be switched to if, else if, and else
            switch (step) // 3 steps as the g r and b separately go up and down
            {
                case 0:
                    g = updateColor(g);
                    break;

                case 1:
                    r = updateColor(r);
                    break;

                case 2:
                    b = updateColor(b);
                    break;

                default: // in case someone changes the code and messes up the steps
                    step = 0;
                    nextColor();
                    break;
            }
            return Color.FromArgb((int)r, (int)g, (int)b);
        }

    }
}
