using Godot;
using System;
using System.Collections.Generic;
using System.Security.Principal;

public partial class Tiling : Sprite2D
{
    int pixelHeight;
    int pixelWidth;
    ImageTexture texture = new ImageTexture();
    public ShaderMaterial material;
    List<Vector2> roots = new List<Vector2>();
    public override void _Ready()
    {
        material = (ShaderMaterial)Material;
        pixelHeight = (int)(GetViewport().GetWindow().Size.Y);
        pixelWidth = (int)(GetViewport().GetWindow().Size.X);
        Image image = Image.Create(pixelWidth, pixelHeight, false, Image.Format.Rgba8);
        texture.SetImage(image);
        Texture = texture;
        SendData(material);
    }
    public override void _Process(double delta)
    {
        // inputs
        Vector2 mouse = GetViewport().GetMousePosition();
        if (Input.IsActionJustPressed("Click"))
        {
            roots.Add(mouse);
        }
        if (Input.IsActionJustPressed("MMB"))
        {

            int id = findClosest();
            if (id != -1)
            {
                roots.RemoveAt(id);
            }
        }
        if (Input.IsActionPressed("RightClick"))
        {
            int id = findClosest();
            if (id != -1)
            {
                roots[id] = mouse;

            }
            
        }
        SendData(material);

    }
    public void SendData(ShaderMaterial m)
    {
        int id = findClosest();
        Vector2[] list = new Vector2[100];
        for (int i = 0; i < roots.Count && i < 100; i++)
        {
            list[i] = roots[i];
        }
        m.SetShaderParameter("roots", list);
        m.SetShaderParameter("valid", Math.Min(roots.Count, 100));
    }
    public int findClosest()
    {
        Vector2 mouse = GetViewport().GetMousePosition();
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
        return id;
    }
}
