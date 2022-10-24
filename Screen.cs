using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameOfLifeFaster
{
    abstract class Screen
    {
        public abstract void InitailizeScreen();
        public abstract void Update();
        public abstract void Draw();
    }
}
