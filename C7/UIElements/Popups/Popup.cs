using Godot;
using System.Collections.Generic;
using System.Diagnostics;

public partial class Popup : TextureRect {
	private static readonly Dictionary<(int, int), ImageTexture> backgroundCache = new();

	// The default values match the sizing of the Civ III popup texture pieces.
	private const int HTILE_SIZE = 16;
	private const int VTILE_SIZE = 16;

	protected void TileHorizontal(Image image, Image left, Image center, Image right, int width, int vOffset) {
		image.BlitRect(left, new Rect2I(new Vector2I(0, 0), new Vector2I(left.GetWidth(), left.GetHeight())), new Vector2I(0, vOffset));

		int leftOffset = HTILE_SIZE;
		for (; leftOffset < width - HTILE_SIZE; leftOffset += HTILE_SIZE) {
			image.BlitRect(center, new Rect2I(new Vector2I(0, 0), new Vector2I(center.GetWidth(), center.GetHeight())), new Vector2I(leftOffset, vOffset));
		}

		leftOffset = width - HTILE_SIZE;
		image.BlitRect(right, new Rect2I(new Vector2I(0, 0), new Vector2I(right.GetWidth(), right.GetHeight())), new Vector2I(leftOffset, vOffset));
	}

	protected void AddTexture(int width, int height) {
		Image image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
		image.Fill(Color.Color8(0, 0, 0, 0));
		this.Texture = ImageTexture.CreateFromImage(image);
	}

	protected void AddBackground(int width, int height, int vOffset = 0) {
		TextureRect background = CreateBackground(width, height);
		background.SetPosition(new Vector2(0, vOffset));
		AddChild(background);
	}

	private TextureRect CreateBackground(int width, int height) {
		TextureRect rect = new TextureRect();

		if (backgroundCache.ContainsKey((width, height))) {
			rect.Texture = backgroundCache[(width, height)];
			return rect;
		}

		Image image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);

		//The pop-up part is the tricky part
		Stopwatch imageTimer = new Stopwatch();
		imageTimer.Start();
		Image topLeftPopup = TextureLoader.Load("popup_background.top_left").GetImage();
		Image topCenterPopup = TextureLoader.Load("popup_background.top_center").GetImage();
		Image topRightPopup = TextureLoader.Load("popup_background.top_right").GetImage();
		Image middleLeftPopup = TextureLoader.Load("popup_background.middle_left").GetImage();
		Image middleCenterPopup = TextureLoader.Load("popup_background.middle_center").GetImage();
		Image middleRightPopup = TextureLoader.Load("popup_background.middle_right").GetImage();
		Image bottomLeftPopup = TextureLoader.Load("popup_background.bottom_left").GetImage();
		Image bottomCenterPopup = TextureLoader.Load("popup_background.bottom_center").GetImage();
		Image bottomRightPopup = TextureLoader.Load("popup_background.bottom_right").GetImage();

		//Upper portion
		TileHorizontal(image, topLeftPopup, topCenterPopup, topRightPopup, width, 0);

		//Middle portion
		int vOffset = VTILE_SIZE;
		for (; vOffset < height - VTILE_SIZE; vOffset += VTILE_SIZE) {
			TileHorizontal(image, middleLeftPopup, middleCenterPopup, middleRightPopup, width, vOffset);
		}

		//Lower portion
		vOffset = height - VTILE_SIZE;
		TileHorizontal(image, bottomLeftPopup, bottomCenterPopup, bottomRightPopup, width, vOffset);

		imageTimer.Stop();
		LogManager.ForContext<Popup>().Debug("Popup background creation time: " + imageTimer.ElapsedMilliseconds + " ms");

		ImageTexture texture = ImageTexture.CreateFromImage(image);
		backgroundCache[(width, height)] = texture;
		rect.Texture = texture;
		return rect;
	}
}
