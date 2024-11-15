using Godot;
using System;

public partial class Circles : Sprite2D
{
    int pixelHeight;
    int pixelWidth;
    ImageTexture texture = new ImageTexture();
    public ShaderMaterial material;
    [Export] Tiling tiling;
    public override void _Ready()
    {
        material = (ShaderMaterial)Material;
        pixelHeight = (int)(GetViewport().GetWindow().Size.Y);
        pixelWidth = (int)(GetViewport().GetWindow().Size.X);
        Image image = Image.Create(pixelWidth, pixelHeight, false, Image.Format.Rgba8);
        texture.SetImage(image);
        Texture = texture;
        SendData();
    }
    public override void _Process(double delta)
    {
        SendData();
    }
    public void SendData(){
        material.SetShaderParameter("idclose", tiling.findClosest());
        tiling.SendData(material);
    }
}
