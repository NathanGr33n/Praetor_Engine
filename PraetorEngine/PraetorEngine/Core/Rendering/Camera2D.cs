using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.CompilerServices;

namespace PraetorEngine.Core.Rendering
{
    /// <summary>
    /// 2D camera with position, rotation, zoom, and viewport management.
    /// Supports smooth interpolation for camera movements.
    /// </summary>
    public sealed class Camera2D
    {
        private Vector2 _position;
        private float _rotation;
        private float _zoom;
        private Vector2 _origin;
        private Viewport _viewport;
        private Matrix _transformMatrix;
        private bool _isDirty;

        public Vector2 Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    _isDirty = true;
                }
            }
        }

        public float Rotation
        {
            get => _rotation;
            set
            {
                var normalized = MathHelper.WrapAngle(value);
                if (_rotation != normalized)
                {
                    _rotation = normalized;
                    _isDirty = true;
                }
            }
        }

        public float Zoom
        {
            get => _zoom;
            set
            {
                var clamped = MathHelper.Clamp(value, MinZoom, MaxZoom);
                if (_zoom != clamped)
                {
                    _zoom = clamped;
                    _isDirty = true;
                }
            }
        }

        public Vector2 Origin
        {
            get => _origin;
            set
            {
                if (_origin != value)
                {
                    _origin = value;
                    _isDirty = true;
                }
            }
        }

        public float MinZoom { get; set; } = 0.1f;
        public float MaxZoom { get; set; } = 10.0f;

        public Matrix TransformMatrix
        {
            get
            {
                if (_isDirty)
                {
                    UpdateTransformMatrix();
                }
                return _transformMatrix;
            }
        }

        public Rectangle ViewBounds => GetViewBounds();

        public Camera2D(Viewport viewport)
        {
            _viewport = viewport;
            _position = Vector2.Zero;
            _rotation = 0f;
            _zoom = 1.0f;
            _origin = new Vector2(viewport.Width / 2f, viewport.Height / 2f);
            _isDirty = true;
        }

        /// <summary>
        /// Updates the viewport (e.g., when window is resized).
        /// </summary>
        public void SetViewport(Viewport viewport)
        {
            _viewport = viewport;
            _origin = new Vector2(viewport.Width / 2f, viewport.Height / 2f);
            _isDirty = true;
        }

        /// <summary>
        /// Moves the camera by the specified amount.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Move(Vector2 delta)
        {
            Position += delta;
        }

        /// <summary>
        /// Centers the camera on a specific position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LookAt(Vector2 target)
        {
            Position = target;
        }

        /// <summary>
        /// Converts screen coordinates to world coordinates.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return Vector2.Transform(screenPosition, Matrix.Invert(TransformMatrix));
        }

        /// <summary>
        /// Converts world coordinates to screen coordinates.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2 WorldToScreen(Vector2 worldPosition)
        {
            return Vector2.Transform(worldPosition, TransformMatrix);
        }

        /// <summary>
        /// Gets the visible area in world coordinates.
        /// </summary>
        public Rectangle GetViewBounds()
        {
            var inverseMatrix = Matrix.Invert(TransformMatrix);
            
            var topLeft = Vector2.Transform(Vector2.Zero, inverseMatrix);
            var topRight = Vector2.Transform(new Vector2(_viewport.Width, 0), inverseMatrix);
            var bottomLeft = Vector2.Transform(new Vector2(0, _viewport.Height), inverseMatrix);
            var bottomRight = Vector2.Transform(new Vector2(_viewport.Width, _viewport.Height), inverseMatrix);

            var min = Vector2.Min(Vector2.Min(topLeft, topRight), Vector2.Min(bottomLeft, bottomRight));
            var max = Vector2.Max(Vector2.Max(topLeft, topRight), Vector2.Max(bottomLeft, bottomRight));

            return new Rectangle((int)min.X, (int)min.Y, (int)(max.X - min.X), (int)(max.Y - min.Y));
        }

        /// <summary>
        /// Checks if a point is visible to the camera.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsVisible(Vector2 worldPosition, float margin = 0)
        {
            var bounds = GetViewBounds();
            bounds.Inflate((int)margin, (int)margin);
            return bounds.Contains(worldPosition);
        }

        /// <summary>
        /// Checks if a rectangle is visible to the camera.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsVisible(Rectangle worldBounds, float margin = 0)
        {
            var viewBounds = GetViewBounds();
            viewBounds.Inflate((int)margin, (int)margin);
            return viewBounds.Intersects(worldBounds);
        }

        private void UpdateTransformMatrix()
        {
            _transformMatrix =
                Matrix.CreateTranslation(new Vector3(-_position.X, -_position.Y, 0)) *
                Matrix.CreateRotationZ(_rotation) *
                Matrix.CreateScale(_zoom, _zoom, 1) *
                Matrix.CreateTranslation(new Vector3(_origin.X, _origin.Y, 0));

            _isDirty = false;
        }

        /// <summary>
        /// Resets camera to default state.
        /// </summary>
        public void Reset()
        {
            _position = Vector2.Zero;
            _rotation = 0f;
            _zoom = 1.0f;
            _origin = new Vector2(_viewport.Width / 2f, _viewport.Height / 2f);
            _isDirty = true;
        }
    }

    /// <summary>
    /// Camera controller with smooth interpolation for movements.
    /// </summary>
    public sealed class SmoothCamera2D
    {
        private readonly Camera2D _camera;
        private Vector2 _targetPosition;
        private float _targetZoom;
        private float _smoothSpeed = 5.0f;

        public Camera2D Camera => _camera;
        public float SmoothSpeed
        {
            get => _smoothSpeed;
            set => _smoothSpeed = MathHelper.Max(0.1f, value);
        }

        public SmoothCamera2D(Camera2D camera)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _targetPosition = camera.Position;
            _targetZoom = camera.Zoom;
        }

        /// <summary>
        /// Sets target position for smooth movement.
        /// </summary>
        public void SetTargetPosition(Vector2 target)
        {
            _targetPosition = target;
        }

        /// <summary>
        /// Sets target zoom for smooth zooming.
        /// </summary>
        public void SetTargetZoom(float zoom)
        {
            _targetZoom = MathHelper.Clamp(zoom, _camera.MinZoom, _camera.MaxZoom);
        }

        /// <summary>
        /// Updates camera position and zoom with smooth interpolation.
        /// </summary>
        public void Update(float deltaTime)
        {
            // Smooth position
            var newPosition = Vector2.Lerp(_camera.Position, _targetPosition, 1.0f - MathF.Exp(-_smoothSpeed * deltaTime));
            _camera.Position = newPosition;

            // Smooth zoom
            var newZoom = MathHelper.Lerp(_camera.Zoom, _targetZoom, 1.0f - MathF.Exp(-_smoothSpeed * deltaTime));
            _camera.Zoom = newZoom;
        }
    }
}
