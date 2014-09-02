#region Using Statements
using System;
using System.Collections.Generic;
using WaveEngine.Common;
using WaveEngine.Common.Graphics;
using WaveEngine.Common.Math;
using WaveEngine.Components.Animation;
using WaveEngine.Components.Cameras;
using WaveEngine.Components.Graphics2D;
using WaveEngine.Components.Graphics3D;
using WaveEngine.Framework;
using WaveEngine.Framework.Graphics;
using WaveEngine.Framework.Physics2D;
using WaveEngine.Framework.Resources;
using WaveEngine.Framework.Services;
#endregion

namespace PerPixelColliderProject
{
    public enum SampleState
    {
        Playing,
        Waiting
    }

    public class MyScene : Scene
    {
        public const int MAXOBSTACLES = 6;
        public const int WAITINGTIME = 2000;
        public const float SCROLLACCELERATION = -3;
        public const float SCROLLSPEED = -150;
        public const float SCROLLWIDTH = 1800;

        private const string TEXTURESHIP = "Content/spaceShip.wpk";
        private const string TEXTURESHIPBURST = "Content/spaceShipBurst.wpk";
        private const string TEXTUREOBSTACLE = "Content/asteroid.wpk";
        private const string TEXTUREGROUND = "Content/landScape.wpk";
        private const string TEXTUREBACKGROUND = "Content/background.wpk";
        private const string TEXTUREEXPLOSION = "Content/explosionSpriteSheet.wpk";
        private const string EXPLOSIONSPRITESHEET = "Content/explosionSpriteSheet.xml";

        public IList<Entity> obstacles;
        public Entity ship;
        public Entity ground, ground2, ground3;
        public Entity explosion;

        private SampleState state;

        public int countDown;

        public SampleState State
        {
            get
            {
                return this.state;
            }

            set
            {
                this.state = value;

                if (state == SampleState.Playing)
                {
                    // Sets playing initial state
                    int i = 0;
                    float step = (SCROLLWIDTH / MAXOBSTACLES);
                    foreach (var obstacle in this.obstacles)
                    {
                        obstacle.IsActive = true;
                        var transform = obstacle.FindComponent<Transform2D>();
                        transform.X = step * (i + 3);
                        transform.Y = (float)WaveServices.Random.NextDouble() * WaveServices.Platform.ScreenHeight;
                        i++;
                    }

                    ScrollBehavior.ScrollSpeed = SCROLLSPEED;
                    this.ground.IsActive = true;
                    this.ground2.IsActive = true;
                    this.ground3.IsActive = true;
                    this.ship.Enabled = true;
                    this.ship.FindComponent<Transform2D>().Y = 0;
                    this.ship.FindComponent<ShipBehavior>().Reset();
                    this.explosion.Enabled = false;
                }
                else if (state == SampleState.Waiting)
                {
                    // Sets waiting initial state
                    foreach (var obstacles in this.obstacles)
                    {
                        obstacles.IsActive = false;
                    }

                    this.ship.Enabled = false;
                    this.ground.IsActive = false;
                    this.ground2.IsActive = false;
                    this.ground3.IsActive = false;

                    this.countDown = 0;
                }
            }
        }

        protected override void CreateScene()
        {
            FixedCamera2D camera2d = new FixedCamera2D("camera");
            EntityManager.Add(camera2d);
                
            this.CreateExplosion();
            this.CreateShip();

            Vector2 topLeftCorner = Vector2.Zero;
            WaveServices.ViewportManager.RecoverPosition(ref topLeftCorner);

            this.ground = this.CreateGround("ground1", topLeftCorner.X);
            this.ground2 = this.CreateGround("ground2", topLeftCorner.X + 1024);
            this.ground3 = this.CreateGround("ground3", topLeftCorner.X + 2048);
            this.CreateObstacles();
            this.CreateBackground();

            this.state = SampleState.Playing;
            ScrollBehavior.ScrollSpeed = SCROLLSPEED;
            
            this.AddSceneBehavior(new CollisionSceneBehavior(), SceneBehavior.Order.PostUpdate);
        }

        private void CreateBackground()
        {
            Vector2 corner = Vector2.Zero;
            WaveServices.ViewportManager.RecoverPosition(ref corner);

            var transform = new Transform2D()
            {
                X = corner.X,
                Y = corner.Y,
                DrawOrder = 1,
                XScale = WaveServices.ViewportManager.ScreenWidth / (WaveServices.ViewportManager.RatioX * (float)256),
                YScale = WaveServices.ViewportManager.ScreenHeight / (WaveServices.ViewportManager.RatioY * (float)256)
            };
            var background = new Entity("backGround")
                .AddComponent(transform)
            .AddComponent(new Sprite(TEXTUREBACKGROUND))
            .AddComponent(new SpriteRenderer(DefaultLayers.Opaque));

            this.EntityManager.Add(background);
        }

        private void CreateObstacles()
        {
            this.obstacles = new List<Entity>();
            float x;
            float step = SCROLLWIDTH / MAXOBSTACLES;
            for (int i = 0; i < MAXOBSTACLES; i++)
            {
                x = step * (i + 3);
                float y = (float)(WaveServices.Random.NextDouble() * WaveServices.ViewportManager.VirtualHeight / WaveServices.ViewportManager.RatioY);
                float scale = ((float)WaveServices.Random.NextDouble() * 2f) + 0.5f;
                var obstacle = new Entity("obstacle_" + i)
                .AddComponent(new Transform2D() { X = x, Y = y, XScale = scale, YScale = scale, Origin = new WaveEngine.Common.Math.Vector2(0.5f, 0.5f) })
                .AddComponent(new PerPixelCollider(TEXTUREOBSTACLE, 0))
                .AddComponent(new Sprite(TEXTUREOBSTACLE))
                .AddComponent(new SpriteRenderer(DefaultLayers.Alpha))
                .AddComponent(new ScrollBehavior(SCROLLWIDTH, true, true));

                this.obstacles.Add(obstacle);

                this.EntityManager.Add(obstacle);
            }
        }

        private Entity CreateGround(string name, float x)
        {
            Vector2 bottomLeft = new Vector2(0, WaveServices.Platform.ScreenHeight);
            WaveServices.ViewportManager.RecoverPosition(ref bottomLeft);

            var ground = new Entity(name)
                .AddComponent(new Transform2D() { X = x, Y = bottomLeft.Y - 128 })
            .AddComponent(new PerPixelCollider(TEXTUREGROUND, 0))
            .AddComponent(new Sprite(TEXTUREGROUND))
            .AddComponent(new SpriteRenderer(DefaultLayers.Alpha))
            .AddComponent(new ScrollBehavior(3072, false, false));

            this.EntityManager.Add(ground);
            return ground;
        }

        private void CreateShip()
        {
            this.ship = new Entity("ship")
                .AddComponent(new Transform2D() { X = 100, Y = 32, Origin = new Vector2(0.5f) })
                .AddComponent(new PerPixelCollider(TEXTURESHIP, 0))
                .AddComponent(new Sprite(TEXTURESHIP))
                .AddComponent(new SpriteRenderer(DefaultLayers.Alpha))
                .AddComponent(new ShipBehavior());

            var shipBurst = new Entity("shipBurst")
            .AddComponent(new Transform2D() { Origin = new Vector2(0.5f) })
                .AddComponent(new Sprite(TEXTURESHIPBURST))
                .AddComponent(new SpriteRenderer(DefaultLayers.Additive));
            shipBurst.Enabled = false;

            this.ship.AddChild(shipBurst);

            this.EntityManager.Add(this.ship);
        }

        private void CreateExplosion()
        {
            this.explosion = new Entity("boom")
                .AddComponent(new Transform2D() { XScale = 3, YScale = 2.5f, Origin = new Vector2(0.5f) })
                .AddComponent(new Sprite(TEXTUREEXPLOSION))
                .AddComponent(Animation2D.Create<TexturePackerGenericXml>(EXPLOSIONSPRITESHEET)
                    .Add("Explosion", new SpriteSheetAnimationSequence() { First = 1, Length = 16, FramesPerSecond = 16 }))
                .AddComponent(new AnimatedSpriteRenderer());
            this.explosion.Enabled = false;

            this.EntityManager.Add(this.explosion);
        }

        public void Explosion()
        {
            // Creates the explosions and adjusts to the ship position.
            this.explosion.Enabled = true;

            var explosionTransform = this.explosion.FindComponent<Transform2D>();
            var shipTransform = this.ship.FindComponent<Transform2D>();

            explosionTransform.X = shipTransform.X;
            explosionTransform.Y = shipTransform.Y;

            var anim2D = this.explosion.FindComponent<Animation2D>();
            anim2D.CurrentAnimation = "Explosion";
            anim2D.Play(false);
        }
    }   
}
