using Box2D.NET;
using LiveCaptionsRain.Core.Windows;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

namespace LiveCaptionsRain.Core.Physics;

public sealed class WordPhysicsWorld : IDisposable
{
    private const float PixelsPerMeter = 100f;
    private const float BoundaryThicknessPixels = 120f;
    private const float PlatformThicknessPixels = 36f;

    private readonly B2WorldId _worldId;
    private readonly List<PhysicsWordBody> _words = [];
    private readonly List<B2BodyId> _boundaryBodies = [];
    private readonly List<B2BodyId> _platformBodies = [];
    private bool _disposed;

    public WordPhysicsWorld(ScreenRect bounds)
    {
        var worldDef = b2DefaultWorldDef();
        worldDef.gravity = new B2Vec2(0, 24f);
        worldDef.enableSleep = true;

        _worldId = b2CreateWorld(worldDef);
        SetBounds(bounds);
    }

    public ScreenRect Bounds { get; private set; }

    public int WordCount => _words.Count;

    public Guid AddWord(
        string text,
        PhysicsRect bounds,
        DateTimeOffset? createdAt = null,
        double initialVelocityX = 0,
        double initialVelocityY = 0,
        bool highFallFractureEnabled = true)
    {
        ThrowIfDisposed();

        var bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_dynamicBody;
        bodyDef.position = ToWorld(bounds.CenterX, bounds.CenterY);
        bodyDef.rotation = b2Rot_identity;
        bodyDef.angularDamping = 0.3f;
        bodyDef.linearDamping = 0.02f;
        bodyDef.enableSleep = true;

        var bodyId = b2CreateBody(_worldId, bodyDef);
        var shapeDef = b2DefaultShapeDef();
        shapeDef.material.friction = 0.78f;
        shapeDef.material.restitution = 0.08f;
        shapeDef.density = 0.6f;
        shapeDef.enableContactEvents = true;
        shapeDef.enableHitEvents = true;

        var box = b2MakeBox(ToWorldLength(bounds.Width / 2d), ToWorldLength(bounds.Height / 2d));
        b2CreatePolygonShape(bodyId, shapeDef, box);
        b2Body_ApplyMassFromShapes(bodyId);
        b2Body_SetLinearVelocity(bodyId, new B2Vec2(ToWorldLength(initialVelocityX), ToWorldLength(initialVelocityY)));

        var id = Guid.NewGuid();
        _words.Add(new PhysicsWordBody(
            id,
            text,
            bounds.Width,
            bounds.Height,
            bodyId,
            createdAt ?? DateTimeOffset.UtcNow,
            bounds.Top,
            highFallFractureEnabled));
        return id;
    }

    public void SetBounds(ScreenRect bounds)
    {
        ThrowIfDisposed();
        Bounds = bounds;
        DestroyBodies(_boundaryBodies);

        var width = Math.Max(1, bounds.Width);
        var height = Math.Max(1, bounds.Height);
        var thickness = BoundaryThicknessPixels;

        _boundaryBodies.Add(CreateStaticBox(width / 2d, height + thickness / 2d, width + thickness * 2d, thickness, "floor"));
        _boundaryBodies.Add(CreateStaticBox(-thickness / 2d, height / 2d, thickness, height + thickness, "left-wall"));
        _boundaryBodies.Add(CreateStaticBox(width + thickness / 2d, height / 2d, thickness, height + thickness, "right-wall"));
    }

    public void SetWindowPlatforms(IEnumerable<PhysicsRect> platforms)
    {
        ThrowIfDisposed();
        DestroyBodies(_platformBodies);

        foreach (var platform in platforms)
        {
            if (platform.Width < 32 || platform.Height < 1)
            {
                continue;
            }

            _platformBodies.Add(CreateStaticBox(
                platform.CenterX,
                platform.Top + PlatformThicknessPixels / 2d,
                platform.Width,
                PlatformThicknessPixels,
                "window-top"));
        }
    }

    public void Step(double deltaSeconds, double randomWindPixelsPerSecond)
    {
        Step(deltaSeconds, _ => new WindVector(randomWindPixelsPerSecond, 0));
    }

    public void Step(double deltaSeconds, Func<PhysicsWordSnapshot, WindVector> windProvider)
    {
        ThrowIfDisposed();
        var clampedDelta = Math.Clamp(deltaSeconds, 1d / 240d, 1d / 20d);

        foreach (var word in _words)
        {
            var wind = windProvider(ToSnapshot(word));
            if (Math.Abs(wind.HorizontalPixelsPerSecond) < 0.001
                && Math.Abs(wind.VerticalPixelsPerSecond) < 0.001)
            {
                continue;
            }

            var force = new B2Vec2(
                ToWorldLength(wind.HorizontalPixelsPerSecond),
                ToWorldLength(wind.VerticalPixelsPerSecond));
            b2Body_ApplyForceToCenter(word.BodyId, force, true);
        }

        b2World_Step(_worldId, (float)clampedDelta, 4);
    }

    public IReadOnlyList<PhysicsWordSnapshot> Snapshot()
    {
        ThrowIfDisposed();
        return _words.Select(ToSnapshot).ToArray();
    }

    public PhysicsWordSnapshot? FindAt(double x, double y)
    {
        ThrowIfDisposed();
        return Snapshot().LastOrDefault(word => word.Bounds.Contains(x, y));
    }

    public void Remove(Guid id)
    {
        ThrowIfDisposed();
        var index = _words.FindIndex(word => word.Id == id);
        if (index < 0)
        {
            return;
        }

        b2DestroyBody(_words[index].BodyId);
        _words.RemoveAt(index);
    }

    public void Clear()
    {
        ThrowIfDisposed();
        foreach (var word in _words)
        {
            b2DestroyBody(word.BodyId);
        }

        _words.Clear();
    }

    public void StartOverflowCleanup(int maxActiveWords, TimeSpan fadeDuration, DateTimeOffset now)
    {
        ThrowIfDisposed();
        var activeWords = _words
            .Where(word => !word.IsDeleting)
            .OrderBy(word => word.CreatedAt)
            .ToArray();
        var excess = activeWords.Length - Math.Max(0, maxActiveWords);
        if (excess <= 0)
        {
            return;
        }

        var deleteAt = now + fadeDuration;
        foreach (var word in activeWords.Take(excess))
        {
            word.DeletionStartedAt = now;
            word.DeleteAt = deleteAt;
        }
    }

    public void ApplyCleanup(int maxActiveWords, TimeSpan lifetime, DateTimeOffset now)
    {
        ThrowIfDisposed();
        var removeIds = _words
            .Where(word => now - word.CreatedAt > lifetime
                || (word.DeleteAt is not null && now >= word.DeleteAt.Value))
            .Select(word => word.Id)
            .ToList();

        foreach (var id in removeIds)
        {
            Remove(id);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Clear();
        DestroyBodies(_platformBodies);
        DestroyBodies(_boundaryBodies);
        b2DestroyWorld(_worldId);
        _disposed = true;
    }

    private B2BodyId CreateStaticBox(double centerX, double centerY, double width, double height, string name)
    {
        var bodyDef = b2DefaultBodyDef();
        bodyDef.type = B2BodyType.b2_staticBody;
        bodyDef.position = ToWorld(centerX, centerY);
        bodyDef.name = name;

        var bodyId = b2CreateBody(_worldId, bodyDef);
        var shapeDef = b2DefaultShapeDef();
        shapeDef.material.friction = 0.9f;
        shapeDef.density = 0;

        var box = b2MakeBox(ToWorldLength(width / 2d), ToWorldLength(height / 2d));
        b2CreatePolygonShape(bodyId, shapeDef, box);
        return bodyId;
    }

    private PhysicsWordSnapshot ToSnapshot(PhysicsWordBody word)
    {
        var position = b2Body_GetPosition(word.BodyId);
        var rotation = b2Body_GetRotation(word.BodyId);
        var centerX = ToPixels(position.X);
        var centerY = ToPixels(position.Y);
        var bounds = new PhysicsRect(centerX - word.Width / 2d, centerY - word.Height / 2d, word.Width, word.Height);
        word.HighestTop = Math.Min(word.HighestTop, bounds.Top);
        var fallDistance = Math.Max(0, bounds.Top - word.HighestTop);
        return new PhysicsWordSnapshot(
            word.Id,
            word.Text,
            bounds,
            Math.Atan2(rotation.s, rotation.c),
            word.CreatedAt,
            FallDistancePixels: fallDistance,
            HighFallFractureEnabled: word.HighFallFractureEnabled,
            IsDeleting: word.IsDeleting,
            DeletionStartedAt: word.DeletionStartedAt,
            DeleteAt: word.DeleteAt);
    }

    private static B2Vec2 ToWorld(double x, double y) => new(ToWorldLength(x), ToWorldLength(y));

    private static float ToWorldLength(double pixels) => (float)(pixels / PixelsPerMeter);

    private static double ToPixels(float meters) => meters * PixelsPerMeter;

    private static void DestroyBodies(List<B2BodyId> bodies)
    {
        foreach (var bodyId in bodies)
        {
            b2DestroyBody(bodyId);
        }

        bodies.Clear();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private sealed class PhysicsWordBody(
        Guid id,
        string text,
        double width,
        double height,
        B2BodyId bodyId,
        DateTimeOffset createdAt,
        double highestTop,
        bool highFallFractureEnabled)
    {
        public Guid Id { get; } = id;

        public string Text { get; } = text;

        public double Width { get; } = width;

        public double Height { get; } = height;

        public B2BodyId BodyId { get; } = bodyId;

        public DateTimeOffset CreatedAt { get; } = createdAt;

        public double HighestTop { get; set; } = highestTop;

        public bool HighFallFractureEnabled { get; } = highFallFractureEnabled;

        public DateTimeOffset? DeletionStartedAt { get; set; }

        public DateTimeOffset? DeleteAt { get; set; }

        public bool IsDeleting => DeleteAt is not null;
    }
}
