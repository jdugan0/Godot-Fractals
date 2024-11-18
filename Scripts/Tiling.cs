using Godot;
using System;
using System.Collections.Generic;

public partial class Tiling : Sprite2D
{
    int pixelHeight;
    int pixelWidth;
    ImageTexture texture = new ImageTexture();
    public ShaderMaterial material;
    List<Vector2> roots = new List<Vector2>();
    Vector2 offset = new Vector2(0, 0);
    float zoom = 0.1f;
    [Export] float speed = 100f;
    bool colorScheme = false;
    bool jakeMode = false;
    bool gradient = false;
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
        Vector2 mouse = GetViewport().GetMousePosition() + new Vector2(-1920/2, -1080/2);
        Vector2 scale = (mouse / 1920 / zoom) + offset;

        if (Input.IsActionJustPressed("Click"))
        {
            roots.Add(scale);
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
                roots[id] = scale;

            }
            
        }
        Vector2 velocity = new Vector2();
        if (Input.IsActionPressed("UP")){
            velocity += Vector2.Up;
        }
        if (Input.IsActionPressed("DOWN")){
            velocity += Vector2.Down;
        }
        if (Input.IsActionPressed("LEFT")){
            velocity += Vector2.Left;
        }
        if (Input.IsActionPressed("RIGHT")){
            velocity += Vector2.Right;
        }
        if (Input.IsActionPressed("ZoomIn")){
            zoom += (float)delta * 1f * zoom;
        }
        if (Input.IsActionPressed("ZoomOut")){
            zoom -= (float)delta * 1f * zoom;
        }
        if (Input.IsActionJustPressed("Home")){
            offset = new Vector2(0,0);
            zoom = 0.100f;
        }
        if (Input.IsActionJustPressed("Color")){
            colorScheme = !colorScheme;
        }
        if (Input.IsActionJustPressed("Jake")){
            jakeMode = !jakeMode;
        }
        if (Input.IsActionJustPressed("Gradient")){
            gradient = !gradient;
        }
        // colorScheme = !colorScheme;
        zoom = Mathf.Clamp(zoom, (float)1e-32, 999999);
        velocity = velocity.Normalized() * (float)delta / zoom * speed;
        offset += velocity;
        // GD.Print(zoom);
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
        m.SetShaderParameter("offset", offset);
        m.SetShaderParameter("zoomFactor", zoom);
        m.SetShaderParameter("colorScheme", colorScheme);
        m.SetShaderParameter("jakeMode", jakeMode);
        m.SetShaderParameter("gradient", gradient);
    }
    public int findClosest()
    {
        Vector2 mouse = GetViewport().GetMousePosition() + new Vector2(-1920/2, -1080/2);
        Vector2 scale = (mouse / 1920 / zoom) + offset;
        float best = float.MaxValue;
        int id = -1;
        for (int i = 0; i < roots.Count; i++)
        {
            if (roots[i].DistanceTo(scale) < best)
            {
                best = roots[i].DistanceTo(scale);
                id = i;
            }
        }
        return id;
    }
}
