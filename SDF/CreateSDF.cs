using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot;

public class CreateSDF
{
	struct QueueElement
	{
		public int idx;
		public int x;
		public int y;
		public int dx;
		public int dy;
		public float distance;
	}
	public static Texture2D ConvertImageToSDF(Texture2D input, int downsampleLevel = 1)
	{
		int mipLevel = 0;
        var pixels = new Color[input.Width * input.Height];
        input.GetData(pixels);
		int downsampleWidth = input.Width / downsampleLevel;
		int downsampleHeight = input.Height / downsampleLevel;
		bool[] closed = new bool[downsampleWidth * downsampleHeight];
		bool[] insideShape = new bool[downsampleWidth * downsampleHeight]; // Track which pixels are inside the shape
		float[] distances = new float[downsampleWidth * downsampleHeight];

		var pq = new PriorityQueue<QueueElement, float>();

		float maxDist = 0;

		void EnqueueNeighbors(int x, int y, int dx = 0, int dy = 0)
		{
			var directions = new List<(int, int)>() {
					(-1,1),		// top left
					(0,1),		// top
					(1,1),		// top right
					(-1,0),		// left
					(1,0),		// right
					(-1,-1),	// bottom left
					(0,-1),		// bottom
					(1,-1),		// bottom right

				};

			foreach (var currentDirection in directions)
			{
				int xx = x + currentDirection.Item1;
				int yy = y + currentDirection.Item2;

				if (xx < 0 || xx >= downsampleWidth || yy < 0 || yy >= downsampleHeight)
				{
					continue;
				}

				int idx = yy * downsampleWidth + xx;

				var isClosed = closed[idx];
				var isInside = insideShape[idx]; // Only process pixels inside the shape

				if (isClosed || !isInside)
				{
					continue;
				}

				bool halfDist = dx == 0 && dy == 0;
				EnqueuePixel(xx, yy, x - xx + dx, y - yy + dy, halfDist: halfDist);
			}

		}

		void EnqueuePixel(int x, int y, int dx, int dy, bool halfDist = false)
		{
			int idx = y * downsampleWidth + x;

			closed[idx] = true;

			var queueElement = new QueueElement()
			{
				x = x,
				y = y,
				dx = dx,
				dy = dy,
				distance = MathF.Sqrt(dx * dx + dy * dy)

			};

			if (halfDist)
			{
				queueElement.distance /= 2;
			}

			distances[idx] = queueElement.distance;

			if (queueElement.distance > maxDist)
				maxDist = queueElement.distance;

			pq.Enqueue(queueElement, queueElement.distance);
		}


		//initialize distances to inf for inside pixels, 0 for outside pixels

		for (int i = 0; i < downsampleWidth * downsampleHeight; i++)
		{
			distances[i] = float.MaxValue;
		}

		for (int i = 0; i < downsampleWidth * downsampleHeight; i++)
		{
			closed[i] = false;
		}

		// First pass: identify which pixels are inside the shape
		for (int y = 0; y < downsampleHeight; y++)
		{
			for (int x = 0; x < downsampleWidth; x++)
			{
				int idx = y * downsampleWidth + x;
                var color = pixels[(y * downsampleLevel) * input.Width + (x * downsampleLevel)];
				insideShape[idx] = color.A > 0; // Mark pixels inside the shape
			}
		}

		// Starting from the bottom left corner, iterate to the right and up
		// y = 0 is the bottom and y = height is the top
		// Only start distance calculation from edge pixels (inside pixels next to outside pixels)

		for (int y = 0; y < downsampleHeight; y++)
		{
			for (int x = 0; x < downsampleWidth; x++)
			{
				int idx = y * downsampleWidth + x;

				if (insideShape[idx])
				{
					// Check if this inside pixel is on the edge (has outside neighbors)
					bool isEdge = false;
					var directions = new List<(int, int)>() {
						(-1,1), (0,1), (1,1), (-1,0), (1,0), (-1,-1), (0,-1), (1,-1)
					};

					foreach (var (dx, dy) in directions)
					{
						int nx = x + dx;
						int ny = y + dy;
						
						if (nx < 0 || nx >= downsampleWidth || ny < 0 || ny >= downsampleHeight)
						{
							isEdge = true; // Border of image counts as edge
							break;
						}
						
						int nidx = ny * downsampleWidth + nx;
						if (!insideShape[nidx])
						{
							isEdge = true; // Has an outside neighbor
							break;
						}
					}

					if (isEdge)
					{
						distances[idx] = 0;
						closed[idx] = true;
						EnqueueNeighbors(x, y);
					}
				}
				else
				{
					// Outside pixels get distance 0 and are marked as closed
					distances[idx] = 0;
					closed[idx] = true;
				}
			}
		}

		while (pq.Count > 0)
		{
			var element = pq.Dequeue();

			EnqueueNeighbors(element.x, element.y, element.dx, element.dy);
		}

		Texture2D sdf = new Texture2D(Core.GraphicsDevice, downsampleWidth, downsampleHeight);
		var sdfPixels = new Color[downsampleWidth * downsampleHeight];
        sdf.SetData(sdfPixels);

		for (int y = 0; y < downsampleHeight; y++)
        {
            for (int x = 0; x < downsampleWidth; x++)
            {
                int idx = y * downsampleWidth + x;

				if (insideShape[idx])
				{
					// Inside pixels: distance represents distance to edge (0 = edge, higher = center)
					byte v = (byte)((distances[idx] / maxDist) * 255);
					Color newPixel = new Color((byte)v, (byte)v, (byte)v, (byte)255);
					sdfPixels[idx] = newPixel;

					if (distances[idx] == float.MaxValue)
					{
						// Unreachable inside pixels (shouldn't happen with proper algorithm)
						sdfPixels[idx] = new Color(255, 255, 0, 255); // Yellow for debugging
					}
				}
				else
				{
					// Outside pixels: set to black (distance 0)
					sdfPixels[idx] = new Color(0, 0, 0, 255);
				}
            }
        }

        sdf.SetData(sdfPixels);

		return sdf;
	}
}