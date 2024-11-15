using Godot;
using System;
using System.Collections.Generic;

public partial class ShaderInit : Sprite2D
{
    int pixelHeight;
    int pixelWidth;
    [Export] float resolutionScale = 0.25f;
    ImageTexture texture = new ImageTexture();
    public ShaderMaterial material;
    List<Vector2> roots = new List<Vector2>();
    public override void _Ready()
    {
        material = (ShaderMaterial)Material;
        pixelHeight = (int)(GetViewport().GetWindow().Size.Y * resolutionScale);
        pixelWidth = (int)(GetViewport().GetWindow().Size.X * resolutionScale);
        Scale = new Vector2(1 / resolutionScale, 1 / resolutionScale);
        Image image = Image.Create(pixelWidth, pixelHeight, false, Image.Format.Rgba8);
        texture.SetImage(image);
        Texture = texture;
        SendData();
    }
    public override void _Process(double delta)
    {
        // inputs
        Vector2 mouse = GetViewport().GetMousePosition();
        
        Vector2 uv = (mouse / GetViewportRect().Size);
        float r1 = GetViewportRect().Size.X / GetViewportRect().Size.Y;
        Vector2 scale1 = new Vector2(uv.X, uv.Y * r1);
        if (Input.IsActionJustPressed("Click"))
        {
            roots.Add(mouse);
            SendData();
            GD.Print(mouse);
        }
        if (Input.IsActionJustPressed("RightClick"))
        {

            float best = float.MaxValue;
            int id = -1;
            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i].DistanceTo(mouse) < best)
                {
                    best = roots[i].DistanceTo(mouse);
                    id = i;
                }
            }
            if (id != -1)
            {
                roots.RemoveAt(id);
            }
            SendData();
        }



    }
    public void SendData()
    {
        Vector2[] list = new Vector2[100];
        for (int i = 0; i < 100; i++)
        {
            if (i + 1 > roots.Count)
            {
                list[i] = new Vector2(-999999999, -999999999);
            }
            else
            {
                list[i] = roots[i];
            }
        }
        material.SetShaderParameter("roots", list);
    }
}
