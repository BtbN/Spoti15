using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using JariZ;

namespace Spoti15
{
    class Spoti15
    {
        private SpotifyAPI api;
        private Responses.CFID cfid;
        Responses.Status currentStatus;
        private Exception initExcpt;

        private LogiLcd lcd;

        private Timer spotTimer;
        private Timer lcdTimer;
        private Timer refreshTimer;


        public Spoti15()
        {
            initExcpt = null;

            InitSpot();

            lcd = new LogiLcd("Spoti15");

            spotTimer = new Timer();
            spotTimer.Interval = 1000;
            spotTimer.Enabled = true;
            spotTimer.Tick += OnSpotTimer;

            lcdTimer = new Timer();
            lcdTimer.Interval = 100;
            lcdTimer.Enabled = true;
            lcdTimer.Tick += OnLcdTimer;

            refreshTimer = new Timer();
            refreshTimer.Interval = 300000;
            refreshTimer.Enabled = true;
            refreshTimer.Tick += OnRefreshTimer;

            UpdateSpot();
            UpdateLcd();
        }

        private void OnSpotTimer(object source, EventArgs e)
        {
            UpdateSpot();
        }

        private bool btnBefore = false;
        private void OnLcdTimer(object source, EventArgs e)
        {
            bool btnNow = lcd.IsButtonPressed(LogiLcd.LcdButton.Mono0);
            if (btnNow && !btnBefore)
                InitSpot();
            btnBefore = btnNow;

            UpdateLcd();
        }

        private void OnRefreshTimer(object source, EventArgs e)
        {
            InitSpot();
        }

        public void Dispose()
        {
            lcd.Dispose();

            cfid = null;
            api = null;

            spotTimer.Enabled = false;
            spotTimer.Dispose();
            spotTimer = null;

            lcdTimer.Enabled = false;
            lcdTimer.Dispose();
            lcdTimer = null;

            refreshTimer.Enabled = false;
            refreshTimer.Dispose();
            refreshTimer = null;

            initExcpt = null;
        }

        private void InitSpot()
        {
            try
            {
                api = new SpotifyAPI(SpotifyAPI.GetOAuth());
                cfid = api.CFID;
                initExcpt = null;
            }
            catch (Exception e)
            {
                initExcpt = e;
            }
        }

        public void UpdateSpot()
        {
            if(initExcpt != null)
                return;

            try
            {
                currentStatus = api.Status;
            }
            catch (Exception e)
            {
                initExcpt = e;
            }
        }

        private uint scrollStep = 0;
        private string scrollText(string input)
        {
            if (input.Length < 26)
            {
                while (input.Length < 26)
                    input = " " + input + " ";

                return input;
            }

            if (input.Length > 26)
            {
                long ocut = input.Length - 26;
                long cut = (scrollStep / 5) % (ocut + 10) - 5;

                if (cut < 0)
                    cut = 0;
                if (cut > ocut)
                    cut = ocut;

                return input.Substring((int)cut);
            }

            return input;
        }

        private bool onBorder(int x1, int y1, int x2, int y2, int x, int y)
        {
            if (x >= x1 && x <= x2 && (y == y1 || y == y2))
                return true;
            if (y >= y1 && y <= y2 && (x == x1 || x == x2))
                return true;
            return false;
        }

        private bool inBorder(int x1, int y1, int x2, int y2, int x, int y)
        {
            if (x > x1 && x < x2 && y > y1 && y < y2)
                return true;
            return false;
        }

        private Byte[] emptyBg = new Byte[LogiLcd.MonoWidth * LogiLcd.MonoHeight];
        public void UpdateLcd()
        {
            Bitmap tmpImg = new Bitmap(LogiLcd.MonoWidth, LogiLcd.MonoHeight);
            using (Graphics g = Graphics.FromImage(tmpImg))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;

                g.FillRectangle(Brushes.Black, 0, 0, tmpImg.Width, tmpImg.Height);
                g.DrawString("Das ist ein Test", new Font("Courier", 8), Brushes.White, 0, 0);
            }

            tmpImg.Save(@"D:\tmpImg.bmp");

            lcd.MonoSetBackground(tmpImg);
            lcd.Update();
            return;

            if (initExcpt != null)
            {
                lcd.MonoSetBackground(emptyBg);
                lcd.MonoSetText(0, "Exception:");
                lcd.MonoSetText(1, initExcpt.GetType().ToString());
                lcd.MonoSetText(2, initExcpt.Message.Substring(0, 26));
                lcd.MonoSetText(3, initExcpt.Message.Substring(26));

                lcd.Update();
                return;
            }

            if (cfid.error != null)
            {
                lcd.MonoSetBackground(emptyBg);
                lcd.MonoSetText(0, "SpotifyError:");
                lcd.MonoSetText(1, cfid.error.message);
                lcd.MonoSetText(2, string.Format("Type: 0x{0}", cfid.error.type));
                lcd.MonoSetText(3, "");

                lcd.Update();
                return;
            }

            if (currentStatus.playing)
            {
                int len = currentStatus.track.length;
                int pos = (int)currentStatus.playing_position;
                double perc = currentStatus.playing_position / currentStatus.track.length;

                Byte[] bg = new Byte[LogiLcd.MonoWidth * LogiLcd.MonoHeight];
                for(int y = 0; y < LogiLcd.MonoHeight; ++y)
                    for (int x = 0; x < LogiLcd.MonoWidth; ++x)
                    {
                        int ap = y * LogiLcd.MonoWidth + x;

                        if (onBorder(3, 22, 156, 26, x, y))
                        {
                            bg[ap] = 255;
                        }
                        else if (inBorder(3, 22, 156, 26, x, y))
                        {
                            double lperc = (x - 4.0) / (151.0);
                            if(lperc < perc)
                                bg[ap] = 255;
                            else
                                bg[ap] = 0;
                        }
                        else
                        {
                            bg[ap] = 0;
                        }
                    }

                lcd.MonoSetBackground(bg);

                lcd.MonoSetText(0, scrollText(currentStatus.track.artist_resource.name + " - " + currentStatus.track.album_resource.name));
                lcd.MonoSetText(1, scrollText(currentStatus.track.track_resource.name));
                lcd.MonoSetText(2, "");
                lcd.MonoSetText(3, scrollText(String.Format("{0}:{1:D2}/{2}:{3:D2}", pos/60, pos%60, len/60, len%60)));
            }
            else
            {
                lcd.MonoSetBackground(emptyBg);
                lcd.MonoSetText(0, scrollText("                            Spoti15                          "));
                lcd.MonoSetText(1, scrollText("Spotify not playing"));
                lcd.MonoSetText(2, scrollText(""));
                lcd.MonoSetText(3, scrollText("                            Spoti15                          "));
            }

            lcd.Update();
            scrollStep += 1;
        }
    }
}
