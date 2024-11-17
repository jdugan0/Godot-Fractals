using Godot;
using System;
using System.Collections.Generic;
using Complex = System.Numerics.Complex;
public partial class Renderer : Sprite2D
{
	// Called when the node enters the scene tree for the first time.
	int pixelHeight;
	int pixelWidth;
	ImageTexture texture = new ImageTexture();
	Gradient gradient = new Gradient();
	public List<Pixel> pixels = new List<Pixel>();
	[Export] float resolutionScale = 0.25f;
	[Export] float zoom;
	public override void _Ready()
	{
		Init();
		
		// GD.Print(pixelHeight);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Update();
	}
	public void Init(){
		pixels.Clear();
		pixelHeight = (int)(GetViewport().GetWindow().Size.Y * resolutionScale);
		pixelWidth = (int)(GetViewport().GetWindow().Size.X * resolutionScale);
		for (int x = 0; x < pixelWidth; x++){
			for (int y = 0; y < pixelHeight; y++){
				Pixel p = new Pixel(new Vector2I(x,y));
				pixels.Add(p);
			}
		}
		Scale = new Vector2(1/resolutionScale, 1/resolutionScale);
	}
	public void Update(){
		Image image = Image.Create(pixelWidth, pixelHeight, false, Image.Format.Rgba8);
		
		foreach(Pixel p in pixels){
			p.color = Colors.Gray;
			Complex c = convert(new Complex(p.pos.X, p.pos.Y));
			if (calculateMandlebrot(c, 100)){
				p.color = Colors.Black;
			}
			image.SetPixel(p.pos.X, p.pos.Y, p.color);
			// GD.Print(c);
		}
		texture.SetImage(image);
		Texture = texture;
	}
	public Complex convert(Complex c){
		return ((c * (1/resolutionScale))
		- new Complex(GetViewport().GetWindow().Size.X/2,GetViewport().GetWindow().Size.Y/2)) /zoom;
	}
	public bool calculateMandlebrot(Complex c, int n){
		Complex r = new Complex();
		for (int i = 0; i < n; i++){
			if (i == 0){
				r = c;
			}
			else{
				r = Complex.Pow(r, 2) + c;
			}
			if (r.Magnitude > 3){
				return false;
			}
		}
		return true;
	}
}
