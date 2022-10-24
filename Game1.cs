using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace GameOfLifeFaster
{
    public class Game1 : Game
    {
        #region fields

        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private readonly Random random = new Random();

        private static Vector2 screen_Dimensions = new Vector2(1920, 1080);
        private static readonly int map_Scale = 20;
        private Vector2 field_Dimensions = new Vector2(16 * map_Scale, 9 * map_Scale);
        private int total_Cells;
        private float cell_Size;

        private Texture2D pixel;
        private Texture2D field_Texture;
        private Texture2D selection_Texture;
        private Texture2D grid_Texture;
        private SpriteFont small_Font;
        private SpriteFont med_Font;
        private SpriteFont big_Font;

        private bool isTicking;
        private float tick_Clock = 0;
        private float tick_Speed = .1f;
        private int generation = 0;
        private double density = 0;
        private int alive_Cells;
        private int dead_Cells;

        private readonly Dictionary<Vector2, int> index_Lookup = new Dictionary<Vector2, int>();
        private bool[] current_States;
        private bool[] next_States;
        private byte[] neighbor_Counts;
        private enum ColorMode { normal, starry, cool, inverted, pink, trippy}
        private ColorMode color_Mode = ColorMode.normal;
        Color[] cell_Colors;
        Color[] grid_Colors;
        Color[] selection_Colors;
        Color Active_Color = Color.Black;
        Color Inactive_Color = Color.AliceBlue;

        private Rectangle zoom_Window;
        private Rectangle zoom_Window_Source;
        private readonly float zoom_Window_Size = 250;
        private float zoom_Window_Source_Size = 5;
        private const int zoom_Speed = 4;

        private readonly List<TextBox> text_Boxs = new List<TextBox>();
        TextBox debug_Box;
        TextBox controls_Box;
        TextBox zoom_Box;
        TextBox title_Box;

        private bool is_Ui = false;
        private bool is_Grid = false;
        private int drawing_Radius = 5;

        #endregion

        #region Main Functions

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = (int)screen_Dimensions.X;
            _graphics.PreferredBackBufferHeight = (int)screen_Dimensions.Y;

            _graphics.IsFullScreen = true;
        }
        protected override void Initialize()
        {
            cell_Size = screen_Dimensions.X / field_Dimensions.X;
            total_Cells = (int)(field_Dimensions.X * field_Dimensions.Y);
            field_Texture = new Texture2D(GraphicsDevice, (int)field_Dimensions.X, (int)field_Dimensions.Y);
            grid_Texture = new Texture2D(GraphicsDevice, (int)screen_Dimensions.X, (int)screen_Dimensions.Y);
            selection_Texture = new Texture2D(GraphicsDevice, (int)field_Dimensions.X, (int)field_Dimensions.Y);

            current_States = new bool[total_Cells];
            next_States = new bool[total_Cells];

            neighbor_Counts = new byte[total_Cells];

            cell_Colors = new Color[total_Cells];
            selection_Colors = new Color[total_Cells];
            grid_Colors = new Color[(int)(screen_Dimensions.X * screen_Dimensions.Y)];
            Array.Fill(selection_Colors, Color.Transparent);

            int index = 0;
            for (int i = 0; i < field_Dimensions.Y; i++)
            {
                for (int j = 0; j < field_Dimensions.X; j++)
                {
                    index_Lookup.Add(new Vector2(j, i), index);
                    ResetColoring(index);
                    index++;
                }
            }

            int count = 0;
            for (int i = 0; i < screen_Dimensions.Y; i++)
            {
                for (int j = 0; j < screen_Dimensions.X; j++)
                {
                    if (j % cell_Size == 0 || i % cell_Size == 0) grid_Colors[count] = new Color(Inactive_Color, 55);

                    count++;
                }
            }

            grid_Texture.SetData(grid_Colors);

            Array.Fill(neighbor_Counts, (byte)0);
            UpdateCellState();
            UpdateTextures();

            base.Initialize();
        }
        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            pixel = new Texture2D(GraphicsDevice, 1, 1);
            Color[] pixel_Data = new Color[] { new Color(255, 255, 255) };
            pixel_Data[0] = Color.White;
            pixel.SetData(pixel_Data);

            small_Font = Content.Load<SpriteFont>("Small");
            med_Font = Content.Load<SpriteFont>("Med");
            big_Font = Content.Load<SpriteFont>("Big");
            
            title_Box = new TextBox(" CONWAYS GAME OF LIFE ", big_Font, new Vector2(20, 10), pixel, 2, Color.AliceBlue, Color.DarkOliveGreen);
            debug_Box = new TextBox(DebugString(), med_Font, pixel, 2, Color.AliceBlue, Color.DarkOliveGreen);
            controls_Box = new TextBox(ControlsString(), med_Font, pixel, 2, Color.AliceBlue, Color.DarkOliveGreen);
            zoom_Box = new TextBox("Zoom Window (<, >)", med_Font, pixel, 2, Color.AliceBlue, Color.DarkOliveGreen);

            text_Boxs.Add(title_Box);
            text_Boxs.Add(debug_Box);
            debug_Box.SetPosition(new Vector2((int)((title_Box.Position.X + title_Box.String_Size.X) - (title_Box.Position.X + debug_Box.String_Size.X)) / 2, title_Box.Position.Y + title_Box.String_Size.Y + 20));
            text_Boxs.Add(controls_Box);
            controls_Box.SetPosition(new Vector2(debug_Box.Position.X, debug_Box.Position.Y + debug_Box.String_Size.Y + 20));
            text_Boxs.Add(zoom_Box);
            zoom_Box.SetPosition(new Vector2(screen_Dimensions.X - (zoom_Box.String_Size.X * 1.1f), 10));

            zoom_Window.X = (int)(screen_Dimensions.X - zoom_Window_Size - 10);
            zoom_Window.Y = (int)(zoom_Box.Position.Y + zoom_Box.String_Size.Y) + 10;
            zoom_Window.Width = (int)zoom_Window_Size;
            zoom_Window.Height = (int)zoom_Window_Size;
        }
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState kState = Keyboard.GetState();
            MouseState mState = Microsoft.Xna.Framework.Input.Mouse.GetState();

            tick_Clock += delta;

            if (isTicking && tick_Clock > tick_Speed)
            {
                CalculateNextState();
                UpdateCellState();
                UpdateTextures();
                tick_Clock = 0;
            }

            Mouse(mState);
            ZoomWindow(mState);
            Inputs(kState, delta);

            debug_Box.Update(DebugString());
            controls_Box.Update(ControlsString());

            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            _spriteBatch.Draw(field_Texture, new Rectangle(0, 0, (int)screen_Dimensions.X, (int)screen_Dimensions.Y), Color.White);
            _spriteBatch.Draw(selection_Texture, new Rectangle(0, 0, (int)screen_Dimensions.X, (int)screen_Dimensions.Y), Color.White);

            if (is_Grid) _spriteBatch.Draw(grid_Texture, new Rectangle(0, 0, (int)screen_Dimensions.X, (int)screen_Dimensions.Y), Color.White);

            #region UI

            if (!is_Ui)
            {
                foreach (TextBox t in text_Boxs)
                {
                    t.Draw(_spriteBatch);
                }

                _spriteBatch.Draw(pixel, new Rectangle(zoom_Window.X - 1, zoom_Window.Y - 1, (int)zoom_Window_Size + 2, (int)zoom_Window_Size + 2), new Color(Color.AliceBlue, 200));
                _spriteBatch.Draw(field_Texture, zoom_Window, zoom_Window_Source, Color.White);
                _spriteBatch.Draw(selection_Texture, zoom_Window, zoom_Window_Source, Color.White);
            }
            else _spriteBatch.DrawString(small_Font, "ui is hidden, press h to bring it back.", new Vector2(10, screen_Dimensions.Y - 30), Color.AliceBlue);

            #endregion

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        #endregion

        #region Functions
        private void ClearCells()
        {
            generation = 0;
            alive_Cells = 0;
            dead_Cells = total_Cells;

            Array.Fill(neighbor_Counts, (byte)0);

            for (int i = 0; i < total_Cells; i++)
            {
                next_States[i] = false;
                current_States[i] = false;
                ResetColoring(i);
            }
            UpdateCellState();
            UpdateTextures();
            cLever = true;
        }
        public void Randomize()
        {
            int random_Number;
            bool random_Bool;

            alive_Cells = 0;
            dead_Cells = 0;

            Array.Fill(neighbor_Counts, (byte)0);

            generation = 0; //  resets generation counter

            for (int i = 0; i < total_Cells; i++)
            {
                random_Number = random.Next(5);

                if (random_Number < 3) random_Bool = false; else random_Bool = true;

                current_States[i] = random_Bool;
                next_States[i] = random_Bool;

                if (random_Bool)
                {
                    alive_Cells++;
                    IncreaseNeighbors(i);
                }
                else dead_Cells++;
                ResetColoring(i);
            }

        }

        public void CalculateNextState()
        {
            generation++;

            for (int i = 0; i < total_Cells; i++)
            {
                ApplyRules(i);
            }
        }
        private void ApplyRules(int index)
        {
            if (neighbor_Counts[index] == 3 && !current_States[index]) next_States[index] = true;
            if (neighbor_Counts[index] < 2 || neighbor_Counts[index] > 3 && current_States[index]) next_States[index] = false;
            Coloring(index);
        }

        bool trip_SwitchX = false;
        private void Coloring(int i)
        {
                switch (color_Mode)
                {
                    case ColorMode.normal:
                        if (current_States[i]) cell_Colors[i] = Active_Color; else cell_Colors[i] = Inactive_Color;
                        break;

                    case ColorMode.starry:
                        if (neighbor_Counts[i] == 3 && !current_States[i])
                        {
                            if (cell_Colors[i].B < 255) cell_Colors[i].B++;
                            cell_Colors[i].G = 255;
                        }
                        if (neighbor_Counts[i] < 2 || neighbor_Counts[i] > 3 && current_States[i])
                        {
                            cell_Colors[i].G = 0;
                        }
                        if(current_States[i] && cell_Colors[i].R < 255) cell_Colors[i].R++;
                        else if(!current_States[i] && cell_Colors[i].R > 0) cell_Colors[i].R--;
                        break;

                    case ColorMode.cool:
                        if (current_States[i]) cell_Colors[i] = Color.White;
                        else
                        {
                            if (cell_Colors[i].R > 25) cell_Colors[i].R -= 25;
                            if (cell_Colors[i].G > 5) cell_Colors[i].G -= 5;
                            if (cell_Colors[i].B > 0) cell_Colors[i].B--;
                        }
                        break;

                    case ColorMode.inverted:
                        if (current_States[i]) cell_Colors[i] = Inactive_Color; else cell_Colors[i] = Active_Color;
                        break;

                    case ColorMode.pink:
                        if (neighbor_Counts[i] == 3 && !current_States[i]) cell_Colors[i] = Color.White;
                        if (!current_States[i])
                        {
                            if(cell_Colors[i].G >= 20)cell_Colors[i].G--;
                            if(cell_Colors[i].B >= 147)cell_Colors[i].B -= 10;
                        }
                        break;
                case ColorMode.trippy:
                    if (i % generation == 0) trip_SwitchX = !trip_SwitchX;
                    if (trip_SwitchX && !current_States[i]) cell_Colors[i] = Color.Goldenrod;
                    else if (!trip_SwitchX && !current_States[i]) cell_Colors[i] = Color.LightCoral;
                    else if (current_States[i]) cell_Colors[i] = Color.AliceBlue;
                    break;
                }
        }
        

        private void ResetColoring(int i)
        {
                switch (color_Mode)
                {
                    case ColorMode.normal:
                        if (current_States[i]) cell_Colors[i] = Active_Color; else cell_Colors[i] = Inactive_Color;
                        break;
                    case ColorMode.cool:
                        if (current_States[i]) cell_Colors[i] = Color.White; else cell_Colors[i] = Color.Black;
                        break;
                    case ColorMode.starry:
                        if (current_States[i]) cell_Colors[i] = new Color(0,255,0); else cell_Colors[i] = Color.Black;
                        break;
                    case ColorMode.inverted:
                        if (current_States[i]) cell_Colors[i] = Inactive_Color; else cell_Colors[i] = Active_Color;
                        break;
                    case ColorMode.pink:
                        if (current_States[i]) cell_Colors[i] = Color.White; else cell_Colors[i] = Color.DeepPink;
                        break;
                case ColorMode.trippy:
                    if (current_States[i]) cell_Colors[i] = Color.White; else cell_Colors[i] = Color.Black;
                    break;
                }
        }

        private void IncreaseNeighbors(int index)
        {
            if (index < neighbor_Counts.Length - 1)
                neighbor_Counts[index + 1]++;

            if (index < neighbor_Counts.Length - 1 - field_Dimensions.X)
                neighbor_Counts[index + (int)field_Dimensions.X]++;

            if (index > 0)
                neighbor_Counts[index - 1]++;

            if (index > field_Dimensions.X)
                neighbor_Counts[index - (int)field_Dimensions.X]++;

            if (index < neighbor_Counts.Length - field_Dimensions.X - 1)
                neighbor_Counts[index + (int)field_Dimensions.X + 1]++;

            if (index < neighbor_Counts.Length - field_Dimensions.X + 1)
                neighbor_Counts[index + (int)field_Dimensions.X - 1]++;

            if (index > field_Dimensions.X - 1)
                neighbor_Counts[index - (int)field_Dimensions.X + 1]++;

            if (index > field_Dimensions.X + 1)
                neighbor_Counts[index - (int)field_Dimensions.X - 1]++;
        }
        private void DecreaseNeighbors(int index)
        {
            if (index < neighbor_Counts.Length - 1)
                neighbor_Counts[index + 1]--;

            if (index < neighbor_Counts.Length - field_Dimensions.X)
                neighbor_Counts[index + (int)field_Dimensions.X]--;

            if (index > 0)
                neighbor_Counts[index - 1]--;

            if (index > field_Dimensions.X)
                neighbor_Counts[index - (int)field_Dimensions.X]--;

            if (index < neighbor_Counts.Length - field_Dimensions.X - 1)
                neighbor_Counts[index + (int)field_Dimensions.X + 1]--;

            if (index < neighbor_Counts.Length - field_Dimensions.X + 1)
                neighbor_Counts[index + (int)field_Dimensions.X - 1]--;

            if (index > field_Dimensions.X - 1)
                neighbor_Counts[index - (int)field_Dimensions.X + 1]--;

            if (index > field_Dimensions.X + 1)
                neighbor_Counts[index - (int)field_Dimensions.X - 1]--;
        }
        public void UpdateCellState()
        {
            for (int i = 0; i < total_Cells; i++)
            {
                if (current_States[i] != next_States[i])    // if cells state changed
                {
                    if (next_States[i])
                    {
                        alive_Cells++;
                        dead_Cells--;
                        IncreaseNeighbors(i);
                    }
                    else
                    {
                        dead_Cells++;
                        alive_Cells--;
                        DecreaseNeighbors(i);
                    }
                    current_States[i] = next_States[i];
                }
            }

            density = (double)alive_Cells / (double)total_Cells;
        }
        public void UpdateTextures()
        {
            selection_Texture.SetData(selection_Colors);
            field_Texture.SetData(cell_Colors);
        }
        
        int index;
        Rectangle drawing_Box;
        public void Mouse(MouseState mState)
        {
            // defining a rectangle centered with the cursors position and size is determined by the pen radius
            drawing_Box.X = (int)(mState.X / cell_Size) - (int)drawing_Radius / 2;
            drawing_Box.Y = (int)(mState.Y / cell_Size) - (int)drawing_Radius / 2;
            drawing_Box.Width = drawing_Radius;
            drawing_Box.Height = drawing_Radius;

            Array.Fill(selection_Colors, Color.Transparent);

            for (int y = 0; y < drawing_Box.Height; y++)
            {
                for (int x = 0; x < drawing_Box.Width; x++)
                {
                    index = index_Lookup.GetValueOrDefault(new Vector2(drawing_Box.X + x, drawing_Box.Y + y));

                    selection_Colors[index] = new Color(255, 255, 0, 55);

                    if (mState.LeftButton == ButtonState.Pressed && !current_States[index])
                    {
                        next_States[index] = true;
                        current_States[index] = true;
                        ResetColoring(index);
                        IncreaseNeighbors(index);
                    }
                    else if (mState.RightButton == ButtonState.Pressed && current_States[index])
                    {
                        next_States[index] = false;
                        current_States[index] = false;
                        ResetColoring(index);
                        DecreaseNeighbors(index);
                    }
                }
            }
            UpdateCellState();
            UpdateTextures();
        }
        public void ZoomWindow(MouseState mState)
        {
            zoom_Window_Source.X = (int)((mState.X / cell_Size) - (zoom_Window_Source_Size / 2));
            zoom_Window_Source.Y = (int)((mState.Y / cell_Size) - (zoom_Window_Source_Size / 2));
            zoom_Window_Source.Width = (int)zoom_Window_Source_Size;
            zoom_Window_Source.Height = (int)zoom_Window_Source_Size;
        }
        private string DebugString()
        {
            string debug_String =
                                "                 Debuging               " +
                                " \n [Tick Speed]: " + tick_Speed +
                                " \n [Generation]: " + generation +
                                " \n [Map Size]: " + field_Dimensions.ToString() +
                                " \n [Alive Cells]: " + alive_Cells +
                                " \n [Dead Cells]: " + dead_Cells +
                                " \n [% Of Living Cells]: " + (float)density * 100 +
                                " \n [Color Mode]: " + color_Mode.ToString() +
                                " \n [Grid]: " + is_Grid.ToString() +
                                " \n [Pen Radius]: " + drawing_Radius;
            return debug_String;
        }
        private string ControlsString()
        {
            string control_String =
                "          Controls" +
                "\n [SPACEBAR] start/stop ticking" +
                "\n [+/-] control tick speed" +
                "\n [R] randomize cells" +
                "\n [C] clear screen" +
                "\n [G] enable grid" +
                "\n [M] switch color mode" +
                "\n [S] step one generation" +
                "\n [Left/Right Keys] zoom window" +
                "\n [Up/Down Keys] pen radius" +
                "\n [H] hide ui";

            return control_String;
        }
        #endregion

        #region Inputs
        private float timer = 0;
        private readonly float timer_Limit = 0.5f;
        private bool mLever, cLever, spaceLever, sLever, rLever, hLever, upLever, downLever, leftLever, rightLever, gLever, plusLever, minusLever;
        private void Inputs(KeyboardState kState, float delta)
        {
            if (kState.IsKeyDown(Keys.Up)
                || kState.IsKeyDown(Keys.Down)
                || kState.IsKeyDown(Keys.Right)
                || kState.IsKeyDown(Keys.Left)
                || kState.IsKeyDown(Keys.OemPlus)
                || kState.IsKeyDown(Keys.OemMinus))
            {
                timer += delta;
            }

            // start, stop
            if (kState.IsKeyDown(Keys.Space) && !spaceLever)
            {
                isTicking = !isTicking;
                spaceLever = true;
            }
            else if (kState.IsKeyUp(Keys.Space) && spaceLever) spaceLever = false;
            // hide ui
            if (kState.IsKeyDown(Keys.H) && !hLever)
            {
                is_Ui = !is_Ui;
                hLever = true;
            }
            else if (kState.IsKeyUp(Keys.H) && hLever) hLever = false;
            // color mode
            if (kState.IsKeyDown(Keys.M) && !mLever && !isTicking)
            {
                int limit = Enum.GetValues(typeof(ColorMode)).Length- 1;
                if ((int)color_Mode < limit) color_Mode++; else color_Mode = ColorMode.normal;
                for (int i = 0; i < total_Cells; i++) ResetColoring(i);
                UpdateTextures();
                mLever = true;
            }
            else if (kState.IsKeyUp(Keys.M) && mLever) mLever = false;
            // clear screen
            if (kState.IsKeyDown(Keys.C) && !cLever)
            {
                ClearCells();
            }
            else if (kState.IsKeyUp(Keys.C) && cLever) cLever = false;
            // randomize screen
            if (kState.IsKeyDown(Keys.R) && !rLever)
            {
                Randomize();
                UpdateCellState();
                UpdateTextures();
                rLever = true;
            }
            else if (kState.IsKeyUp(Keys.R) && rLever) rLever = false;
            // step one generation
            if (kState.IsKeyDown(Keys.S) && !sLever)
            {
                CalculateNextState();
                UpdateCellState();
                UpdateTextures();
                sLever = true;
            }
            else if (kState.IsKeyUp(Keys.S) && sLever) sLever = false;
            // zoom window
            if (kState.IsKeyDown(Keys.Left) && !leftLever || kState.IsKeyDown(Keys.Left) && timer >= timer_Limit)
            {
                if (zoom_Window_Source_Size > 75) return;
                zoom_Window_Source_Size += zoom_Speed;
                leftLever = true;
            }
            else if (kState.IsKeyUp(Keys.Left) && leftLever)
            {
                timer = 0;
                leftLever = false;
            }
            if (kState.IsKeyDown(Keys.Right) && !rightLever || kState.IsKeyDown(Keys.Right) && timer >= timer_Limit)
            {
                if (zoom_Window_Source_Size < 5) return;
                zoom_Window_Source_Size -= zoom_Speed;
                rightLever = true;
            }
            else if (kState.IsKeyUp(Keys.Right) && rightLever)
            {
                timer = 0;
                rightLever = false;
            }
            // pen radius
            if (kState.IsKeyDown(Keys.Up) && !upLever || kState.IsKeyDown(Keys.Up) && timer >= timer_Limit)
            {
                if (drawing_Radius < 50) drawing_Radius++;
                upLever = true;
            }
            else if (kState.IsKeyUp(Keys.Up) && upLever)
            {
                timer = 0;
                upLever = false;
            }
            if (kState.IsKeyDown(Keys.Down) && !downLever || kState.IsKeyDown(Keys.Down) && timer > timer_Limit)
            {
                if (drawing_Radius > 1) drawing_Radius--;
                downLever = true;
            }
            else if (kState.IsKeyUp(Keys.Down) && downLever)
            {
                timer = 0;
                downLever = false;
            }
            // enable grid
            if (kState.IsKeyDown(Keys.G) && !gLever)
            {
                is_Grid = !is_Grid;
                gLever = true;
            }
            else if (kState.IsKeyUp(Keys.G) && gLever) gLever = false;
            // tick increment
            if (kState.IsKeyDown(Keys.OemPlus) && !plusLever || kState.IsKeyDown(Keys.OemPlus) && timer >= timer_Limit)
            {
                tick_Speed += 0.01f;
                plusLever = true;
            }
            else if (kState.IsKeyUp(Keys.OemPlus) && plusLever)
            {
                timer = 0;
                plusLever = false;
            }
            if (kState.IsKeyDown(Keys.OemMinus) && !minusLever || kState.IsKeyDown(Keys.OemMinus) && timer >= timer_Limit)
            {
                if (tick_Speed > 0.01f) tick_Speed -= 0.01f;
                else tick_Speed = 0.01f;
                minusLever = true;
            }
            else if (kState.IsKeyUp(Keys.OemMinus) && minusLever)
            {
                timer = 0;
                minusLever = false;
            }
        }

        #endregion
    }
}
