// ============================================================
// CERINTE NUGET — instaleaza inainte de compilare:
//   Install-Package Microsoft.Web.WebView2
//
// In Package Manager Console:
//   Tools → NuGet Package Manager → Package Manager Console
//   Install-Package Microsoft.Web.WebView2
// ============================================================

using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace MotorCautare
{

    // ====================================================================
    // BrowserTab — TabPage complet cu WebView2, bara nav si pagina start
    // ====================================================================
    public class BrowserTab : TabPage
    {
        public WebView2 Browser { get; private set; }
        public Panel PanelNav { get; private set; }
        public TextBox TxtUrl { get; private set; }
        public Button BtnGo { get; private set; }
        public Button BtnBack { get; private set; }
        public Button BtnFwd { get; private set; }
        public Button BtnRef { get; private set; }
        public Panel PanelHome { get; private set; }
        public TextBox TxtAcasa { get; private set; }

        public event EventHandler<string> TabTitleChanged;

        private Font _fontLogo;
        private float _logoX, _logoY, _lastW, _lastH;
        private bool _webviewGata = false;
        private string _urlPending = null;

        private static readonly string LogoText = "WinSearch";
        private static readonly Color[] LogoCulori =
        {
            Color.FromArgb(66,  133, 244),
            Color.FromArgb(234, 67,  53),
            Color.FromArgb(251, 188, 4),
            Color.FromArgb(52,  168, 83),
            Color.FromArgb(66,  133, 244),
            Color.FromArgb(234, 67,  53),
            Color.FromArgb(251, 188, 4),
            Color.FromArgb(52,  168, 83),
            Color.FromArgb(66,  133, 244),
        };

        public BrowserTab(string titlu = "Tab nou") : base(titlu)
        {
            _fontLogo = new Font("Segoe UI Light", 56f, FontStyle.Regular, GraphicsUnit.Point);
            BuildUI();
            _ = InitWebView2Async();  // initializare async — nu blocheaza UI-ul
        }

        private void BuildUI()
        {
            this.BackColor = Color.White;

            // ── bara de navigare ──────────────────────────────────────────
            PanelNav = new Panel();
            PanelNav.Dock = DockStyle.Top;
            PanelNav.Height = 48;
            PanelNav.BackColor = Color.White;
            PanelNav.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(Color.FromArgb(218, 220, 224), 1),
                    0, PanelNav.Height - 1, PanelNav.Width, PanelNav.Height - 1);
            PanelNav.Resize += (s, e) => RepoziNav();

            BtnBack = NavBtn("◄", 6);
            BtnBack.Enabled = false;
            BtnBack.Click += (s, e) => { if (Browser.CanGoBack) Browser.GoBack(); };

            BtnFwd = NavBtn("►", 46);
            BtnFwd.Enabled = false;
            BtnFwd.Click += (s, e) => { if (Browser.CanGoForward) Browser.GoForward(); };

            BtnRef = NavBtn("↺", 86);
            BtnRef.Font = new Font("Segoe UI", 11f);
            BtnRef.Click += (s, e) =>
            {
                if (BtnRef.Text == "✕") Browser.Stop();
                else Browser.Reload();
            };

            Button btnHome = NavBtn("⌂", 126);
            btnHome.Font = new Font("Segoe UI", 12f);
            btnHome.Click += (s, e) => MergiAcasa();

            TxtUrl = new TextBox();
            TxtUrl.Font = new Font("Segoe UI", 10.5f);
            TxtUrl.BorderStyle = BorderStyle.FixedSingle;
            TxtUrl.Location = new Point(170, 10);
            TxtUrl.Size = new Size(600, 28);
            TxtUrl.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { Naviga(TxtUrl.Text); e.SuppressKeyPress = true; }
            };

            BtnGo = new Button();
            BtnGo.Text = "Caută";
            BtnGo.Size = new Size(68, 28);
            BtnGo.FlatStyle = FlatStyle.Flat;
            BtnGo.FlatAppearance.BorderSize = 0;
            BtnGo.BackColor = Color.FromArgb(26, 115, 232);
            BtnGo.ForeColor = Color.White;
            BtnGo.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            BtnGo.Cursor = Cursors.Hand;
            BtnGo.Click += (s, e) => Naviga(TxtUrl.Text);

            PanelNav.Controls.AddRange(new Control[] { BtnBack, BtnFwd, BtnRef, btnHome, TxtUrl, BtnGo });

            // ── pagina de start ───────────────────────────────────────────
            PanelHome = new Panel();
            PanelHome.Dock = DockStyle.Fill;
            PanelHome.BackColor = Color.White;
            PanelHome.Paint += PanelHome_Paint;
            PanelHome.Resize += (s, e) => RepoziHome();

            TxtAcasa = new TextBox();
            TxtAcasa.Font = new Font("Segoe UI", 14f);
            TxtAcasa.BorderStyle = BorderStyle.FixedSingle;
            TxtAcasa.Size = new Size(520, 36);
            TxtAcasa.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { Naviga(TxtAcasa.Text); e.SuppressKeyPress = true; }
            };

            Button btnCA = new Button();
            btnCA.Name = "btnCA";
            btnCA.Text = "Caută cu WinSearch";
            btnCA.Size = new Size(185, 36);
            btnCA.FlatStyle = FlatStyle.Flat;
            btnCA.FlatAppearance.BorderColor = Color.FromArgb(210, 210, 210);
            btnCA.BackColor = Color.FromArgb(248, 249, 250);
            btnCA.ForeColor = Color.FromArgb(60, 60, 60);
            btnCA.Font = new Font("Segoe UI", 10f);
            btnCA.Cursor = Cursors.Hand;
            btnCA.Click += (s, e) => Naviga(TxtAcasa.Text);

            Button btnNR = new Button();
            btnNR.Name = "btnNR";
            btnNR.Text = "Ma simt norocos";
            btnNR.Size = new Size(155, 36);
            btnNR.FlatStyle = FlatStyle.Flat;
            btnNR.FlatAppearance.BorderColor = Color.FromArgb(210, 210, 210);
            btnNR.BackColor = Color.FromArgb(248, 249, 250);
            btnNR.ForeColor = Color.FromArgb(60, 60, 60);
            btnNR.Font = new Font("Segoe UI", 10f);
            btnNR.Cursor = Cursors.Hand;
            btnNR.Click += (s, e) =>
            {
                string[] t = { "YouTube", "Wikipedia", "C# tutorials", "Visual Studio 2022" };
                Naviga(t[new Random().Next(t.Length)]);
            };

            PanelHome.Controls.AddRange(new Control[] { TxtAcasa, btnCA, btnNR });

            // ── WebView2 ──────────────────────────────────────────────────
            // WebView2 necesita initializare async — il cream acum dar
            // il folosim doar dupa ce InitWebView2Async() se termina
            Browser = new WebView2();
            Browser.Dock = DockStyle.Fill;
            Browser.Visible = false;

            // Ordinea Dock: Fill primul, Top dupa
            this.Controls.Add(Browser);
            this.Controls.Add(PanelHome);
            this.Controls.Add(PanelNav);
        }

        // ── initializare WebView2 (async, motor Chromium) ─────────────────
        private async Task InitWebView2Async()
        {
            try
            {
                // EnsureCoreWebView2Async initializeaza motorul Edge Chromium
                await Browser.EnsureCoreWebView2Async(null);

                // Setari dupa initializare
                Browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
                Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                Browser.CoreWebView2.Settings.IsZoomControlEnabled = true;

                // Evenimentele se ataseaza DUPA initializare
                Browser.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
                Browser.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
                Browser.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
                Browser.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;

                _webviewGata = true;

                // Daca exista un URL in asteptare (cerut inainte de initializare)
                if (_urlPending != null)
                {
                    Browser.CoreWebView2.Navigate(_urlPending);
                    _urlPending = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "WebView2 Runtime nu este instalat!\n\n" +
                    "Descarca de la: https://developer.microsoft.com/microsoft-edge/webview2/\n\n" +
                    "Detalii: " + ex.Message,
                    "Eroare WebView2",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ── navigare ─────────────────────────────────────────────────────
        public void Naviga(string termen)
        {
            termen = termen?.Trim();
            if (string.IsNullOrEmpty(termen)) return;

            bool esteUrl = termen.StartsWith("http://") ||
                           termen.StartsWith("https://") ||
                           (termen.Contains(".") && !termen.Contains(" "));

            string url = esteUrl
                ? (termen.StartsWith("http") ? termen : "https://" + termen)
                : "https://www.google.com/search?q=" + Uri.EscapeDataString(termen);

            TxtUrl.Text = url;
            PanelHome.Visible = false;
            Browser.Visible = true;

            if (_webviewGata)
                Browser.CoreWebView2.Navigate(url);
            else
                _urlPending = url;  // stocam URL-ul pana se termina initializarea
        }

        public void MergiAcasa()
        {
            Browser.Visible = false;
            PanelHome.Visible = true;
            this.Text = "Tab nou";
            TxtUrl.Text = "";
            RepoziHome();
        }

        // ── events WebView2 ───────────────────────────────────────────────
        private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            // Invoke pentru ca evenimentul poate veni de pe alt thread
            if (InvokeRequired)
                Invoke(new Action(() => CoreWebView2_NavigationStarting(sender, e)));
            else
            {
                BtnRef.Text = "✕";
                TxtUrl.Text = e.Uri;
                NavigationStarted?.Invoke(this, e.Uri);
            }
        }

        private void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (InvokeRequired)
                Invoke(new Action(() => CoreWebView2_NavigationCompleted(sender, e)));
            else
            {
                BtnRef.Text = "↺";
                BtnBack.Enabled = Browser.CanGoBack;
                BtnFwd.Enabled = Browser.CanGoForward;
                TxtUrl.Text = Browser.Source?.ToString() ?? "";
            }
        }

        private void CoreWebView2_DocumentTitleChanged(object sender, object e)
        {
            if (InvokeRequired)
                Invoke(new Action(() => CoreWebView2_DocumentTitleChanged(sender, e)));
            else
            {
                string titlu = Browser.CoreWebView2.DocumentTitle ?? "";
                this.Text = titlu.Length > 18 ? titlu.Substring(0, 16) + "…" : (titlu != "" ? titlu : "Tab nou");
                TabTitleChanged?.Invoke(this, titlu);
            }
        }

        // Deschide linkurile cu target="_blank" in tab nou in loc de fereastra noua
        private void CoreWebView2_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            if (InvokeRequired)
                Invoke(new Action(() => TabNouCerut?.Invoke(this, e.Uri)));
            else
                TabNouCerut?.Invoke(this, e.Uri);
        }

        public event EventHandler<string> NavigationStarted;
        public event EventHandler<string> TabNouCerut;

        // ── helper buton navigare ─────────────────────────────────────────
        private Button NavBtn(string txt, int x)
        {
            var b = new Button();
            b.Text = txt;
            b.Location = new Point(x, 10);
            b.Size = new Size(34, 28);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            b.Font = new Font("Segoe UI", 10f);
            b.Cursor = Cursors.Hand;
            return b;
        }

        // ── repositionare bara nav ────────────────────────────────────────
        private void RepoziNav()
        {
            const int x = 170;
            int lat = PanelNav.Width - x - BtnGo.Width - 14;
            TxtUrl.Width = Math.Max(lat, 80);
            TxtUrl.Location = new Point(x, 10);
            BtnGo.Location = new Point(x + TxtUrl.Width + 6, 10);
        }

        // ── logo ─────────────────────────────────────────────────────────
        private void PanelHome_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            if (PanelHome.Width != _lastW || PanelHome.Height != _lastH)
            {
                float total = 0;
                foreach (char c in LogoText)
                    total += g.MeasureString(c.ToString(), _fontLogo).Width - 8f;
                _logoX = PanelHome.Width / 2f - total / 2f;
                _logoY = PanelHome.Height / 2f - 130;
                _lastW = PanelHome.Width;
                _lastH = PanelHome.Height;
            }

            float x = _logoX;
            for (int i = 0; i < LogoText.Length; i++)
            {
                string lit = LogoText[i].ToString();
                using (var br = new SolidBrush(LogoCulori[i]))
                    g.DrawString(lit, _fontLogo, br, x, _logoY);
                x += g.MeasureString(lit, _fontLogo).Width - 8f;
            }
        }

        private void RepoziHome()
        {
            int cx = PanelHome.Width / 2;
            int cy = PanelHome.Height / 2;

            TxtAcasa.Location = new Point(cx - TxtAcasa.Width / 2, cy - 18);

            Control b1 = PanelHome.Controls["btnCA"];
            Control b2 = PanelHome.Controls["btnNR"];
            if (b1 != null && b2 != null)
            {
                int tw = b1.Width + 12 + b2.Width;
                b1.Location = new Point(cx - tw / 2, cy + 38);
                b2.Location = new Point(cx - tw / 2 + b1.Width + 12, cy + 38);
            }

            _lastW = -1;
            PanelHome.Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _fontLogo?.Dispose(); Browser?.Dispose(); }
            base.Dispose(disposing);
        }
    }

    // ====================================================================
    // GoogleForm — fereastra principala cu TabControl
    // ====================================================================
    public class GoogleForm : Form
    {
        private TabControl tabControl;
        private Button btnNouTab;
        private Label lblStatus;

        public GoogleForm()
        {
            InitializeComponent();
            AdaugaTab();
        }

        private void InitializeComponent()
        {
            this.Icon = new Icon("logo.ico");
            this.SuspendLayout();
            

            // ── bara superioara cu "+ Tab nou" ────────────────────────────
            Panel panelSus = new Panel();
            panelSus.Dock = DockStyle.Top;
            panelSus.Height = 36;
            panelSus.BackColor = Color.FromArgb(235, 235, 235);

            btnNouTab = new Button();
            btnNouTab.Text = "+  Tab nou";
            btnNouTab.Dock = DockStyle.Right;
            btnNouTab.Width = 110;
            btnNouTab.FlatStyle = FlatStyle.Flat;
            btnNouTab.FlatAppearance.BorderSize = 0;
            btnNouTab.BackColor = Color.FromArgb(26, 115, 232);
            btnNouTab.ForeColor = Color.White;
            btnNouTab.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnNouTab.Cursor = Cursors.Hand;
            btnNouTab.Click += (s, e) => AdaugaTab();

            panelSus.Controls.Add(btnNouTab);

            // ── tab control ───────────────────────────────────────────────
            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = new Font("Segoe UI", 9.5f);
            tabControl.Padding = new Point(16, 4);
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.MouseDown += TabControl_MouseDown;
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            // ── status bar ────────────────────────────────────────────────
            lblStatus = new Label();
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Height = 22;
            lblStatus.BackColor = Color.FromArgb(248, 248, 248);
            lblStatus.Font = new Font("Segoe UI", 8f);
            lblStatus.ForeColor = Color.Gray;
            lblStatus.Text = "  Bun venit la WinSearch";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;

            // ── form ─────────────────────────────────────────────────────
            this.Text = "WinSearch";
            this.Size = new Size(1200, 750);
            this.MinimumSize = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            this.Controls.Add(tabControl);
            this.Controls.Add(lblStatus);
            this.Controls.Add(panelSus);

            this.ResumeLayout(false);
        }

        // Clasa pentru istoric

        public class HistoryItem
        {
        public string Titlu { get; set; }
        public string Url { get; set; }
        public DateTime Data { get; set; }
        }

        // ── adauga tab nou ────────────────────────────────────────────────
        private void AdaugaTab(string url = null)
        {
            var tab = new BrowserTab("Tab nou");

            tab.TabTitleChanged += (s, titlu) =>
            {
                if (tabControl.SelectedTab == tab)
                {
                    this.Text = titlu + " — WinSearch";
                    lblStatus.Text = "  " + titlu;
                }
            };

            tab.NavigationStarted += (s, uri) =>
            {
                if (tabControl.SelectedTab == tab)
                    lblStatus.Text = "  Se incarca: " + uri;
            };

            // Linkurile cu target="_blank" deschid tab nou
            tab.TabNouCerut += (s, uri) => AdaugaTab(uri);

            tabControl.TabPages.Add(tab);
            tabControl.SelectedTab = tab;

            if (url != null) tab.Naviga(url);
        }

        // ── actualizeaza titlul ferestrei la schimbarea tab-ului ─────────
        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab is BrowserTab tab)
            {
                string titlu = tab.Text;
                this.Text = titlu + " — WinSearch";
                lblStatus.Text = "  " + titlu;
            }
        }

        // ── desenare tab personalizat cu buton X ──────────────────────────
        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabPage page = tabControl.TabPages[e.Index];
            bool selectat = e.Index == tabControl.SelectedIndex;

            Color bg = selectat ? Color.White : Color.FromArgb(228, 228, 228);
            using (var br = new SolidBrush(bg))
                e.Graphics.FillRectangle(br, e.Bounds);

            using (var p = new Pen(selectat ? Color.White : Color.FromArgb(200, 200, 200)))
                e.Graphics.DrawLine(p, e.Bounds.Left, e.Bounds.Bottom - 1,
                                       e.Bounds.Right, e.Bounds.Bottom - 1);

            Rectangle rText = new Rectangle(e.Bounds.X + 8, e.Bounds.Y + 3,
                                             e.Bounds.Width - 26, e.Bounds.Height - 3);
            TextRenderer.DrawText(e.Graphics, page.Text, tabControl.Font, rText,
                Color.FromArgb(40, 40, 40),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            Rectangle rX = XRect(e.Bounds);
            Color xColor = selectat ? Color.FromArgb(100, 100, 100) : Color.FromArgb(160, 160, 160);
            using (var br = new SolidBrush(xColor))
                e.Graphics.DrawString("×", new Font("Segoe UI", 10f, FontStyle.Bold), br, rX.X, rX.Y - 1);
        }

        private Rectangle XRect(Rectangle b) =>
            new Rectangle(b.Right - 20, b.Top + 6, 16, 16);

        // ── click pe X inchide tab ────────────────────────────────────────
        private void TabControl_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControl.TabPages.Count; i++)
            {
                if (!XRect(tabControl.GetTabRect(i)).Contains(e.Location)) continue;

                if (tabControl.TabPages.Count == 1)
                {
                    ((BrowserTab)tabControl.TabPages[0]).MergiAcasa();
                    return;
                }

                var tab = (BrowserTab)tabControl.TabPages[i];
                tabControl.TabPages.Remove(tab);
                tab.Dispose();
                return;
            }
        }
    }
}