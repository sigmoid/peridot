using Microsoft.Xna.Framework;
using Peridot.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace peridot.EntityComponentScene.Physics
{
	public class PolygonColliderComponent : Component
	{
		public List<Vector2> Vertices;

		private RigidbodyComponent _rigidBody;

		public override void Initialize()
		{
			_rigidBody = RequireComponent<RigidbodyComponent>();
			base.Initialize();
		}

		public void SetVertices(List<Vector2> vertices)
		{
			Vertices = vertices;

			_rigidBody.UpdateCollider();
		}
	}


}
