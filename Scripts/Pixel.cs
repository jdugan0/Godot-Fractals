using Godot;
using System;

public partial class Pixel
{
    public Vector2I pos = new Vector2I();
    public Color color = new Color();
    public Pixel(Vector2I pos){
        this.pos = pos;
    }
}
