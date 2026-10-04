using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SnakeClub {
    enum GameState { Ready, Running, Paused, Over, Won }
    class Game {
        public const int Size = 20;
        public List<Point> Snake = new List<Point>();
        public Queue<Point> Turns = new Queue<Point>();
        public Point Direction = new Point(1, 0);
        public Point Food;
        public int Score;
        public GameState State = GameState.Ready;
        readonly Random random = new Random();
        public Game() { Reset(); State = GameState.Ready; }
        public void Reset() {
            Snake.Clear(); Snake.Add(new Point(9,10)); Snake.Add(new Point(8,10)); Snake.Add(new Point(7,10));
            Direction = new Point(1,0); Turns.Clear(); Score = 0; State = GameState.Running; PlaceFood();
        }
        public void Turn(Point next) {
            if (State != GameState.Running || Turns.Count >= 2) return;
            Point previous = Direction;
            foreach (Point p in Turns) previous = p;
            if (next == previous || (next.X == -previous.X && next.Y == -previous.Y)) return;
            Turns.Enqueue(next);
        }
        void PlaceFood() {
            var free = new List<Point>();
            for (int y=0;y<Size;y++) for(int x=0;x<Size;x++) {
                Point p=new Point(x,y); if(!Snake.Contains(p)) free.Add(p);
            }
            if (free.Count == 0) { State = GameState.Won; return; }
            Food = free[random.Next(free.Count)];
        }
        public void Tick() {
            if (State != GameState.Running) return;
            if (Turns.Count > 0) Direction = Turns.Dequeue();
            Point head = new Point(Snake[0].X + Direction.X, Snake[0].Y + Direction.Y);
            bool eat = head == Food;
            if(head.X<0 || head.Y<0 || head.X>=Size || head.Y>=Size) { State=GameState.Over; return; }
            int count = Snake.Count - (eat ? 0 : 1);
            for(int i=0;i<count;i++) if(Snake[i]==head) { State=GameState.Over; return; }
            Snake.Insert(0,head);
            if(eat) { Score+=10; PlaceFood(); } else Snake.RemoveAt(Snake.Count-1);
        }
        public static void Verify() {
            Game g=new Game(); g.Reset(); g.Food=new Point(10,10); g.Tick();
            Check(g.Score==10 && g.Snake.Count==4,"food");
            g.Turn(new Point(-1,0)); Check(g.Turns.Count==0,"reverse");
            g.Turn(new Point(0,-1)); g.Turn(new Point(-1,0)); g.Tick(); g.Tick();
            Check(g.Snake[0]==new Point(9,9),"queued turns");
            g.State=GameState.Paused; Point p=g.Snake[0]; g.Tick(); Check(g.Snake[0]==p,"pause");
            g.Reset(); g.Snake=new List<Point>{new Point(19,10),new Point(18,10)}; g.Tick(); Check(g.State==GameState.Over,"wall");
            g.Reset(); g.Food=new Point(0,0); g.Snake=new List<Point>{new Point(10,10),new Point(10,11),new Point(11,11),new Point(11,10)};
            g.Tick(); Check(g.State==GameState.Running,"tail");
            g.Snake=new List<Point>{new Point(10,10),new Point(11,10),new Point(11,11),new Point(10,11)};
            g.Tick(); Check(g.State==GameState.Over,"self collision");
            g.Reset(); Check(g.Score==0 && g.Snake.Count==3,"restart");
            g.Snake.Clear(); for(int y=0;y<Size;y++) for(int x=0;x<Size;x++) if(x!=1 || y!=0)g.Snake.Add(new Point(x,y));
            g.Food=new Point(1,0);g.Direction=new Point(1,0);g.Tick();Check(g.State==GameState.Won,"win");
        }
        static void Check(bool ok,string message) { if(!ok)throw new Exception(message); }
    }
    class Board : Control {
        public Game Game;
        public Board(Game game) { Game=game; DoubleBuffered=true; BackColor=Color.FromArgb(17,29,21); }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e); Graphics g=e.Graphics; g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            float c=Math.Min(Width,Height)/20f, ox=(Width-c*20)/2, oy=(Height-c*20)/2;
            using(Pen pen=new Pen(Color.FromArgb(36,52,38)))for(int i=0;i<=20;i++){
                g.DrawLine(pen,ox+i*c,oy,ox+i*c,oy+c*20);g.DrawLine(pen,ox,oy+i*c,ox+c*20,oy+i*c);
            }
            if(Game.State!=GameState.Won)using(Brush b=new SolidBrush(Color.FromArgb(255,149,116)))
                g.FillEllipse(b,ox+(Game.Food.X+.2f)*c,oy+(Game.Food.Y+.2f)*c,c*.6f,c*.6f);
            for(int i=Game.Snake.Count-1;i>=0;i--) {
                Point p=Game.Snake[i]; using(Brush b=new SolidBrush(i==0?Color.FromArgb(211,249,155):Color.FromArgb(145,188,96)))
                    g.FillRectangle(b,ox+p.X*c+2,oy+p.Y*c+2,c-4,c-4);
            }
            Point head=Game.Snake[0]; using(Brush b=new SolidBrush(Color.FromArgb(32,50,25)))foreach(int s in new int[]{-1,1})
                g.FillEllipse(b,ox+(head.X+.5f)*c+Game.Direction.X*c*.2f+Game.Direction.Y*s*c*.16f-2,oy+(head.Y+.5f)*c+Game.Direction.Y*c*.2f+Game.Direction.X*s*c*.16f-2,4,4);
            if(Game.State!=GameState.Running) {
                using(Brush b=new SolidBrush(Color.FromArgb(224,16,27,20)))g.FillRectangle(b,0,0,Width,Height);
                string title=Game.State==GameState.Ready?"准备好开吃了吗？":Game.State==GameState.Paused?"休息一下。":Game.State==GameState.Won?"恭喜通关！":"这一口，先歇一下。";
                string subtitle=Game.State==GameState.Ready?"点击开始游戏，或按空格":Game.State==GameState.Paused?"按空格继续游戏":"本局得分 "+Game.Score+" · 按 R 再来一局";
                using(Font f=new Font("Microsoft YaHei UI",22,FontStyle.Bold))TextRenderer.DrawText(g,title,f,new Rectangle(0,Height/2-50,Width,50),Color.FromArgb(237,245,223),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
                using(Font f=new Font("Microsoft YaHei UI",11))TextRenderer.DrawText(g,subtitle,f,new Rectangle(0,Height/2+6,Width,40),Color.FromArgb(160,175,153),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
            }
        }
    }
    class MainWindow : Form {
        readonly Game game=new Game();
        readonly Timer timer=new Timer();
        readonly Board board;
        readonly Label scoreLabel=new Label(),bestLabel=new Label(),status=new Label();
        readonly Button start=new Button(),pause=new Button();
        readonly Button difficulty=new Button();
        readonly Color ink=Color.FromArgb(237,245,223),lime=Color.FromArgb(195,237,121);
        readonly string recordPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SnakeClub","best.txt");
        int best,baseSpeed=125,difficultyIndex=1;
        public MainWindow() {
            Text="贪吃蛇 · Snake Club"; ClientSize=new Size(940,700); MinimumSize=new Size(860,680);
            StartPosition=FormStartPosition.CenterScreen; BackColor=Color.FromArgb(16,25,19); ForeColor=ink;
            Font=new Font("Microsoft YaHei UI",10); KeyPreview=true;
            try{int.TryParse(File.ReadAllText(recordPath),out best);best=Math.Max(0,best);}catch{}
            Label brand=LabelAt("●  SNAKE CLUB",28,20,500,30,12,lime,true);
            LabelAt("再吃一口。",28,62,520,64,32,ink,true);
            LabelAt("转个弯，吃颗果子。让小蛇长大，也让纪录更进一步。",30,130,560,30,10,Color.FromArgb(160,175,153),false);
            board=new Board(game);board.SetBounds(30,178,480,480);Controls.Add(board);
            LabelAt("YOUR NEXT PERSONAL BEST",550,78,340,28,10,lime,true);
            LabelAt("本局得分",550,128,150,24,10,ink,false);LabelAt("最高纪录",735,128,150,24,10,ink,false);
            ConfigureLabel(scoreLabel,"0",550,158,150,60,32,lime,true);ConfigureLabel(bestLabel,best.ToString(),735,158,150,60,32,ink,true);
            LabelAt("找到你的节奏",550,246,330,30,13,ink,true);LabelAt("游戏速度",550,288,100,30,10,ink,false);
            ConfigureButton(difficulty,"标准 · 点击切换",675,286,180,32,false);
            difficulty.Click+=delegate{difficultyIndex=(difficultyIndex+1)%3;difficulty.Text=new string[]{"悠闲","标准","挑战"}[difficultyIndex]+" · 点击切换";};
            LabelAt("速度在下一局生效。\n吃得越多，速度会稍微加快。",550,334,330,60,10,Color.FromArgb(160,175,153),false);
            ConfigureButton(start,"开始游戏 →",550,412,305,46,true);start.Click+=delegate {StartGame();};
            ConfigureButton(pause,"Ⅱ 暂停",550,472,145,42,false);pause.Enabled=false;pause.Click+=delegate{TogglePause();};
            Button restart=new Button();ConfigureButton(restart,"↻ 重新开始",710,472,145,42,false);restart.Click+=delegate{StartGame();};
            LabelAt("方向键 / WASD  移动\n空格  暂停或继续    R  重新开始\n\n小诀窍：给自己留一点转弯的空间。",550,546,340,115,10,Color.FromArgb(160,175,153),false);
            ConfigureLabel(status,"等待开始 · 每颗果子 +10 分",30,668,550,25,9,Color.FromArgb(160,175,153),false);status.Anchor=AnchorStyles.Left|AnchorStyles.Bottom;
            Resize+=delegate{LayoutBoard();};LayoutBoard();
            timer.Tick+=delegate{game.Tick();UpdateGame();};
            Deactivate+=delegate{if(game.State==GameState.Running)TogglePause();};
            FormClosing+=delegate{timer.Stop();timer.Dispose();};
        }
        void LayoutBoard(){int length=Math.Min(480,Math.Min(ClientSize.Height-220,ClientSize.Width-430));board.SetBounds(30,178,length,length);}
        Label LabelAt(string text,int x,int y,int w,int h,int size,Color color,bool bold){Label l=new Label();ConfigureLabel(l,text,x,y,w,h,size,color,bold);return l;}
        void ConfigureLabel(Label l,string text,int x,int y,int w,int h,int size,Color color,bool bold){l.Text=text;l.SetBounds(x,y,w,h);l.ForeColor=color;l.Font=new Font("Microsoft YaHei UI",size,bold?FontStyle.Bold:FontStyle.Regular);Controls.Add(l);}
        void ConfigureButton(Button b,string text,int x,int y,int w,int h,bool primary){b.Text=text;b.SetBounds(x,y,w,h);b.FlatStyle=FlatStyle.Flat;b.FlatAppearance.BorderColor=Color.FromArgb(70,88,62);b.BackColor=primary?lime:Color.FromArgb(25,36,28);b.ForeColor=primary?Color.FromArgb(28,43,21):ink;Controls.Add(b);}
        void StartGame(){timer.Stop();baseSpeed=new int[]{180,125,80}[difficultyIndex];game.Reset();start.Text="新的一局 →";UpdateGame();timer.Start();board.Focus();}
        void TogglePause(){if(game.State==GameState.Running){game.State=GameState.Paused;timer.Stop();}else if(game.State==GameState.Paused){game.State=GameState.Running;timer.Start();}UpdateGame();}
        void UpdateGame(){
            scoreLabel.Text=game.Score.ToString();
            if(game.Score>best){best=game.Score;bestLabel.Text=best.ToString();try{Directory.CreateDirectory(Path.GetDirectoryName(recordPath));File.WriteAllText(recordPath,best.ToString());}catch{}}
            timer.Interval=Math.Max(45,baseSpeed-game.Score/50*6);
            pause.Enabled=game.State==GameState.Running||game.State==GameState.Paused;pause.Text=game.State==GameState.Paused?"▶ 继续":"Ⅱ 暂停";
            if(game.State==GameState.Over||game.State==GameState.Won){timer.Stop();status.Text=game.State==GameState.Won?"恭喜通关！":"游戏结束 · 按 R 再来一局";}
            else status.Text=game.State==GameState.Paused?"已暂停":"正在游戏 · 每颗果子 +10 分";
            board.Invalidate();
        }
        protected override bool ProcessCmdKey(ref Message msg,Keys keyData){
            switch(keyData){
                case Keys.Up:case Keys.W:game.Turn(new Point(0,-1));return true;
                case Keys.Down:case Keys.S:game.Turn(new Point(0,1));return true;
                case Keys.Left:case Keys.A:game.Turn(new Point(-1,0));return true;
                case Keys.Right:case Keys.D:game.Turn(new Point(1,0));return true;
                case Keys.Space:if(game.State==GameState.Ready)StartGame();else TogglePause();return true;
                case Keys.R:StartGame();return true;
            }
            return base.ProcessCmdKey(ref msg,keyData);
        }
    }
    static class Program {
        [STAThread] static int Main(string[] args) {
            if(args.Length>0 && args[0]=="--self-test") { try{Game.Verify();return 0;}catch{return 1;} }
            if(args.Length==2 && args[0]=="--render-preview") {
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                using(MainWindow window=new MainWindow())using(Bitmap bitmap=new Bitmap(window.ClientSize.Width,window.ClientSize.Height)) {
                    using(Graphics graphics=Graphics.FromImage(bitmap))graphics.Clear(window.BackColor);
                    foreach(Control control in window.Controls){IntPtr handle=control.Handle;control.DrawToBitmap(bitmap,control.Bounds);}
                    bitmap.Save(args[1],System.Drawing.Imaging.ImageFormat.Png);
                }
                return 0;
            }
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new MainWindow());return 0;
        }
    }
}
