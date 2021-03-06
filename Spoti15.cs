﻿using System;
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

        private uint scrollStep = 0;

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
            scrollStep += 1;
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

        private Bitmap bgBitmap = new Bitmap(LogiLcd.MonoWidth, LogiLcd.MonoHeight);
        private Font mainFont = new Font(Program.GetFontFamily("8pxbus"), 10, GraphicsUnit.Pixel);
        private Color bgColor = Color.Black;
        private Color fgColor = Color.White;
        private Brush bgBrush = Brushes.Black;
        private Brush fgBrush = Brushes.White;

        private void SetupGraphics(Graphics g)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.PageUnit = GraphicsUnit.Pixel;
            g.TextContrast = 0;

            g.Clear(bgColor);
        }

        private void DrawText(Graphics g, int line, string text, Font fnt, int offset = 0)
        {
            int x = offset;
            int y = line * 10;
            TextRenderer.DrawText(g, text, fnt, new Point(x, y), fgColor);
        }

        private void DrawTextScroll(Graphics g, int line, string text, Font fnt, bool center = true)
        {
            Size textSize = TextRenderer.MeasureText(text, fnt);

            if (textSize.Width <= LogiLcd.MonoWidth + 2)
            {
                if (center)
                {
                    int offset = (LogiLcd.MonoWidth - textSize.Width) / 2;
                    DrawText(g, line, text, fnt, offset);
                }
                else
                {
                    DrawText(g, line, text, fnt);
                }

                return;
            }

            int pxstep = 4;
            int speed = 5;
            int prewait = 5;
            int postwait = 5;

            int olen = textSize.Width - LogiLcd.MonoWidth;
            int len = pxstep * (int)((scrollStep / speed) % ((olen / pxstep) + prewait + postwait) - prewait);
            if (len < 0)
                len = 0;
            if (len > olen)
                len = olen;

            DrawText(g, line, text, fnt, -len);
        }

        private void DrawTextScroll(Graphics g, int line, string text, bool center = true)
        {
            DrawTextScroll(g, line, text, mainFont, center);
        }

        private void DrawText(Graphics g, int line, string text, int offset = 0)
        {
            DrawText(g, line, text, mainFont, offset);
        }

        private void DoRender()
        {
            lcd.MonoSetBackground(bgBitmap);
            lcd.Update();
        }

        private Byte[] emptyBg = new Byte[LogiLcd.MonoWidth * LogiLcd.MonoHeight];
        public void UpdateLcd()
        {
            if (initExcpt != null)
            {
                using (Graphics g = Graphics.FromImage(bgBitmap))
                {
                    SetupGraphics(g);
                    DrawText(g, 0, "Exception:");
                    DrawText(g, 1, initExcpt.GetType().ToString());
                    DrawTextScroll(g, 2, initExcpt.Message, false);
                }

                DoRender();
                return;
            }

            if (cfid.error != null)
            {
                using (Graphics g = Graphics.FromImage(bgBitmap))
                {
                    SetupGraphics(g);
                    DrawText(g, 0, "SpotifyError:");
                    DrawTextScroll(g, 1, cfid.error.message, false);
                    DrawText(g, 2, String.Format("Type: 0x{0}", cfid.error.type));
                }

                DoRender();
                return;
            }

            using (Graphics g = Graphics.FromImage(bgBitmap))
            {
                SetupGraphics(g);

                try
                {
                    int len = currentStatus.track.length;
                    int pos = (int)currentStatus.playing_position;
                    double perc = currentStatus.playing_position / currentStatus.track.length;

                    DrawTextScroll(g, 0, currentStatus.track.artist_resource.name + " - " + currentStatus.track.album_resource.name);
                    DrawTextScroll(g, 1, currentStatus.track.track_resource.name);
                    DrawTextScroll(g, 3, String.Format("{0}:{1:D2}/{2}:{3:D2}", pos / 60, pos % 60, len / 60, len % 60));

                    g.DrawRectangle(Pens.White, 3, 24, LogiLcd.MonoWidth - 6, 4);
                    g.FillRectangle(Brushes.White, 3, 24, (int)((LogiLcd.MonoWidth - 6) * perc), 4);

                    if (currentStatus.playing)
                    {
                        g.FillPolygon(Brushes.White, new Point[] { new Point(3, 40), new Point(3, 30), new Point(8, 35) });
                    }
                    else
                    {
                        g.FillRectangle(Brushes.White, new Rectangle(3, 32, 2, 7));
                        g.FillRectangle(Brushes.White, new Rectangle(6, 32, 2, 7));
                    }
                }
                catch (NullReferenceException)
                {
                    g.Clear(bgColor);
                    DrawTextScroll(g, 1, "No track information available", false);
                }
            }

            DoRender();
        }
    }
}
