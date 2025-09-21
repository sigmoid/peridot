using Genbox.VelcroPhysics.Collision.Shapes;
using Genbox.VelcroPhysics.Dynamics;
using Genbox.VelcroPhysics.Factories;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using peridot.Physics;
using Peridot;
using Peridot.Components;
using System;
using System.Linq;

namespace peridot.EntityComponentScene.Physics
{
	public enum BodyType
	{
		Static,
		Dynamic,
		Kinematic
	}

	public class RigidbodyComponent : Component
	{
		public Vector2 LocalPosition { get; set; }
		public Vector2 Position => Entity.Position + LocalPosition;
		public bool IsStatic { get; set; } = false; // Indicates if the Rigidbody is static or dynamic

		public Body Body { get; set; }

		public BodyType BodyType { get; set; } = BodyType.Dynamic;

		PolygonColliderComponent _collider;

		Fixture _fixture;

		public RigidbodyComponent(BodyType bodyType)
		{
			BodyType = bodyType;
		}

		public override void Initialize()
		{
			base.Initialize();
			_collider = RequireComponent<PolygonColliderComponent>();

			if (Body == null)
			{
				Body = BodyFactory.CreateBody(Core.Physics.World, PhysicsSystem.ToSimUnits(Entity.Position), 0, GetGenBoxBodyType(BodyType));

				Body.FixedRotation = false;

				var vertices = _collider.Vertices;

				if (vertices != null && vertices.Count >= 3)
				{ 					
					Genbox.VelcroPhysics.Shared.Vertices verts = new Genbox.VelcroPhysics.Shared.Vertices(vertices.Select(v => new Microsoft.Xna.Framework.Vector2(v.X, v.Y)).ToList());
					var shape = new Genbox.VelcroPhysics.Collision.Shapes.PolygonShape(verts, 1f);
					_fixture = Body.AddFixture(shape);
				}
				else
				{
					Logger.Error("PolygonColliderComponent must have at least 3 vertices to create a polygon shape.");
				}
			}
		}

		public override void Update(GameTime gameTime)
		{
			if (Body != null && Entity != null)
			{
				Entity.Position = PhysicsSystem.ToDisplayUnits(new Vector2(Body.Position.X, Body.Position.Y));
				Entity.Rotation =  Body.Rotation;
			}
			base.Update(gameTime);
		}

		private Genbox.VelcroPhysics.Dynamics.BodyType GetGenBoxBodyType(BodyType type)
		{
			switch(type)
			{
				case BodyType.Static:
					return Genbox.VelcroPhysics.Dynamics.BodyType.Static;
				case BodyType.Dynamic:
					return Genbox.VelcroPhysics.Dynamics.BodyType.Dynamic;
				case BodyType.Kinematic:
					return Genbox.VelcroPhysics.Dynamics.BodyType.Kinematic;
				default:
					return Genbox.VelcroPhysics.Dynamics.BodyType.Dynamic;
			}
		}

		public void UpdateCollider()
		{
			var vertices = _collider.Vertices;

			Body.RemoveFixture(_fixture);

			Genbox.VelcroPhysics.Shared.Vertices verts = new Genbox.VelcroPhysics.Shared.Vertices(vertices.Select(v => new Microsoft.Xna.Framework.Vector2(v.X, v.Y)).ToList());
			var shape = new Genbox.VelcroPhysics.Collision.Shapes.PolygonShape(verts, 1f);
			_fixture = Body.AddFixture(shape);
		}
	}
}
