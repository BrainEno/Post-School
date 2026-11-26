using System;
using System.Collections.Generic;
using UnityEngine;

namespace PostSchool.Character
{
    /// <summary>
    /// Accumulates a skinned mesh out of rings, tubes and shells, carrying bone weights as it goes.
    ///
    /// The point of this builder is that limbs are <em>tubes with ring topology</em>, not capsules
    /// stacked end to end: the silhouette can taper, bend and pinch at a joint, and every vertex is
    /// weighted along the way, so one continuous surface deforms instead of several primitives
    /// sliding over each other. That difference is the whole reason the figures can be animated.
    /// </summary>
    public sealed class SkinnedMeshBuilder
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<BoneWeight4> _weights = new List<BoneWeight4>();
        private readonly List<List<int>> _submeshes = new List<List<int>>();

        public int VertexCount => _vertices.Count;

        public int TriangleCount
        {
            get
            {
                int total = 0;
                foreach (List<int> submesh in _submeshes) total += submesh.Count;
                return total / 3;
            }
        }

        /// <summary>How many material slots the built mesh needs (at least one).</summary>
        public int SubmeshCount => Mathf.Max(1, _submeshes.Count);

        /// <summary>
        /// Routes every following triangle into one material slot. Calls must come in ascending order,
        /// because the built mesh's submeshes are written in the order they were opened.
        /// </summary>
        public void BeginSurface(CharacterSurface surface)
        {
            int index = (int)surface;
            while (_submeshes.Count <= index) _submeshes.Add(new List<int>());
            _currentSurface = index;
        }

        private int _currentSurface;

        /// <summary>
        /// A standalone box (a bag, a shoe block) placed and rotated in figure space, with one weight per
        /// corner. Kept here so every hard shape goes through the same weight-carrying path as the tubes.
        /// </summary>
        public void AddOrientedBox(Vector3 center, Vector3 halfExtents, Quaternion rotation, BoneWeight4 weight)
        {
            int startIndex = _vertices.Count;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? -halfExtents.x : halfExtents.x,
                    (i & 2) == 0 ? -halfExtents.y : halfExtents.y,
                    (i & 4) == 0 ? -halfExtents.z : halfExtents.z);
                Vector3 rotated = rotation * corner;
                _vertices.Add(center + rotated);
                _normals.Add(rotated.normalized);
                _uvs.Add(new Vector2((i & 1) == 0 ? 0f : 1f, (i & 2) == 0 ? 0f : 1f));
                _weights.Add(weight);
            }

            int[] quads = { 0, 2, 3, 1, 4, 5, 7, 6, 0, 1, 5, 4, 2, 6, 7, 3, 0, 4, 6, 2, 1, 3, 7, 5 };
            for (int q = 0; q < quads.Length; q += 4)
            {
                AddTriangle(startIndex + quads[q], startIndex + quads[q + 1], startIndex + quads[q + 2]);
                AddTriangle(startIndex + quads[q], startIndex + quads[q + 2], startIndex + quads[q + 3]);
            }
        }

        /// <summary>
        /// A tapered tube through a list of joints. Each joint has a position and a radius; the tube is
        /// closed with caps, and each ring is weighted by blending the bones that meet at that joint.
        /// </summary>
        public void AddTube(IReadOnlyList<Vector3> spine, IReadOnlyList<float> radii, IReadOnlyList<BoneWeight4> ringWeights, int sides, bool capStart = true, bool capEnd = true, float squashX = 1f, float squashZ = 1f)
        {
            if (spine == null || radii == null || ringWeights == null) throw new ArgumentNullException(nameof(spine));
            if (spine.Count < 2) throw new ArgumentException("A tube needs at least two joints.", nameof(spine));
            if (radii.Count != spine.Count) throw new ArgumentException("Every joint needs a radius.", nameof(radii));
            if (ringWeights.Count != spine.Count) throw new ArgumentException("Every joint needs a weight.", nameof(ringWeights));
            if (sides < 4) throw new ArgumentOutOfRangeException(nameof(sides), sides, "A tube needs at least four sides.");

            int ringCount = spine.Count;
            int startIndex = _vertices.Count;

            for (int ring = 0; ring < ringCount; ring++)
            {
                Vector3 forward = RingDirection(spine, ring);
                Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
                Vector3 right = Vector3.Cross(up, forward).normalized;
                up = Vector3.Cross(forward, right).normalized;

                for (int side = 0; side < sides; side++)
                {
                    float angle = side / (float)sides * Mathf.PI * 2f;
                    Vector3 offset = (right * (Mathf.Cos(angle) * squashX) + up * (Mathf.Sin(angle) * squashZ)) * radii[ring];
                    _vertices.Add(spine[ring] + offset);
                    _normals.Add(offset.sqrMagnitude > 1e-10f ? offset.normalized : right);
                    _uvs.Add(new Vector2(side / (float)sides, ring / (float)(ringCount - 1)));
                    _weights.Add(ringWeights[ring]);
                }
            }

            for (int ring = 0; ring < ringCount - 1; ring++)
            {
                for (int side = 0; side < sides; side++)
                {
                    int nextSide = (side + 1) % sides;
                    int a = startIndex + ring * sides + side;
                    int b = startIndex + ring * sides + nextSide;
                    int c = startIndex + (ring + 1) * sides + side;
                    int d = startIndex + (ring + 1) * sides + nextSide;
                    AddTriangle(a, c, b);
                    AddTriangle(b, c, d);
                }
            }

            if (capStart) AddCap(startIndex, sides, spine[0], spine[0] - RingDirection(spine, 0) * 0.01f, ringWeights[0]);
            if (capEnd)
            {
                int lastRing = startIndex + (ringCount - 1) * sides;
                AddCap(lastRing, sides, spine[ringCount - 1], spine[ringCount - 1] + RingDirection(spine, ringCount - 1) * 0.01f, ringWeights[ringCount - 1]);
            }
        }

        /// <summary>An ellipsoid shell, weighted by a caller-supplied function of the local position.</summary>
        public void AddEllipsoid(Vector3 center, Vector3 radius, int longitude, int latitude, Func<Vector3, BoneWeight4> weightAt)
        {
            if (longitude < 6) throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Need at least six longitudes.");
            if (latitude < 4) throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Need at least four latitudes.");

            int startIndex = _vertices.Count;
            for (int lat = 0; lat <= latitude; lat++)
            {
                float phi = lat / (float)latitude * Mathf.PI;
                for (int lon = 0; lon < longitude; lon++)
                {
                    float theta = lon / (float)longitude * Mathf.PI * 2f;
                    Vector3 unit = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                    Vector3 local = Vector3.Scale(unit, radius);
                    Vector3 position = center + local;
                    _vertices.Add(position);
                    _normals.Add(Vector3.Scale(unit, new Vector3(1f / Mathf.Max(radius.x, 1e-4f), 1f / Mathf.Max(radius.y, 1e-4f), 1f / Mathf.Max(radius.z, 1e-4f))).normalized);
                    _uvs.Add(new Vector2(lon / (float)longitude, 1f - lat / (float)latitude));
                    _weights.Add(weightAt(position));
                }
            }

            for (int lat = 0; lat < latitude; lat++)
            {
                for (int lon = 0; lon < longitude; lon++)
                {
                    int nextLon = (lon + 1) % longitude;
                    int a = startIndex + lat * longitude + lon;
                    int b = startIndex + lat * longitude + nextLon;
                    int c = startIndex + (lat + 1) * longitude + lon;
                    int d = startIndex + (lat + 1) * longitude + nextLon;
                    AddTriangle(a, c, b);
                    AddTriangle(b, c, d);
                }
            }
        }

        /// <summary>A flat quad facing the given normal, used for drawn marks and cloth panels.</summary>
        public void AddQuad(Vector3 center, Vector3 right, Vector3 up, float halfWidth, float halfHeight, BoneWeight4 weight, float normalOffset = 0f)
        {
            Vector3 normal = Vector3.Cross(up, right).normalized;
            Vector3 offset = normal * normalOffset;
            int startIndex = _vertices.Count;

            _vertices.Add(center - right * halfWidth - up * halfHeight + offset);
            _vertices.Add(center + right * halfWidth - up * halfHeight + offset);
            _vertices.Add(center + right * halfWidth + up * halfHeight + offset);
            _vertices.Add(center - right * halfWidth + up * halfHeight + offset);
            for (int i = 0; i < 4; i++)
            {
                _normals.Add(normal);
                _weights.Add(weight);
            }

            _uvs.Add(new Vector2(0f, 0f));
            _uvs.Add(new Vector2(1f, 0f));
            _uvs.Add(new Vector2(1f, 1f));
            _uvs.Add(new Vector2(0f, 1f));

            AddTriangle(startIndex, startIndex + 2, startIndex + 1);
            AddTriangle(startIndex, startIndex + 3, startIndex + 2);
        }

        /// <summary>An axis-aligned box with per-vertex weights, for bags, soles and other hard shapes.</summary>
        public void AddBox(Vector3 center, Vector3 halfExtents, BoneWeight4 weight)
        {
            int startIndex = _vertices.Count;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3(
                    (i & 1) == 0 ? -halfExtents.x : halfExtents.x,
                    (i & 2) == 0 ? -halfExtents.y : halfExtents.y,
                    (i & 4) == 0 ? -halfExtents.z : halfExtents.z);
                _vertices.Add(center + corner);
                _normals.Add(corner.normalized);
                _uvs.Add(new Vector2((i & 1) == 0 ? 0f : 1f, (i & 2) == 0 ? 0f : 1f));
                _weights.Add(weight);
            }

            int[] quads =
            {
                0, 2, 3, 1, // bottom
                4, 5, 7, 6, // top
                0, 1, 5, 4, // front
                2, 6, 7, 3, // back
                0, 4, 6, 2, // left
                1, 3, 7, 5, // right
            };

            for (int q = 0; q < quads.Length; q += 4)
            {
                int a = startIndex + quads[q];
                int b = startIndex + quads[q + 1];
                int c = startIndex + quads[q + 2];
                int d = startIndex + quads[q + 3];
                AddTriangle(a, b, c);
                AddTriangle(a, c, d);
            }
        }

        /// <summary>Bakes everything into a mesh. Normals are recomputed so tubes read as smooth surfaces.</summary>
        public Mesh Build(string meshName, bool recalculateNormals = true)
        {
            if (_vertices.Count == 0) throw new InvalidOperationException($"Mesh '{meshName}' has no geometry.");

            Mesh mesh = new Mesh { name = meshName };
            if (_vertices.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            WriteSubmeshes(mesh);
            if (recalculateNormals) mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        /// <summary>The weights, in the same order as the built mesh's vertices.</summary>
        public BoneWeight4[] BuildWeights()
        {
            return _weights.ToArray();
        }

        /// <summary>Converts the built weights into Unity's own format for a SkinnedMeshRenderer.</summary>
        public BoneWeight[] BuildUnityWeights()
        {
            BoneWeight[] unity = new BoneWeight[_weights.Count];
            for (int i = 0; i < _weights.Count; i++)
            {
                BoneWeight4 source = _weights[i];
                unity[i] = new BoneWeight
                {
                    boneIndex0 = source.bone0,
                    boneIndex1 = source.bone1,
                    boneIndex2 = source.bone2,
                    boneIndex3 = source.bone3,
                    weight0 = source.weight0,
                    weight1 = source.weight1,
                    weight2 = source.weight2,
                    weight3 = source.weight3,
                };
            }

            return unity;
        }

        private void AddTriangle(int a, int b, int c)
        {
            while (_submeshes.Count <= _currentSurface) _submeshes.Add(new List<int>());
            _submeshes[_currentSurface].Add(a);
            _submeshes[_currentSurface].Add(b);
            _submeshes[_currentSurface].Add(c);
        }

        private void WriteSubmeshes(Mesh mesh)
        {
            mesh.subMeshCount = SubmeshCount;
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                mesh.SetTriangles(i < _submeshes.Count ? _submeshes[i] : new List<int>(), i);
            }
        }

        private void AddCap(int ringStart, int sides, Vector3 center, Vector3 tip, BoneWeight4 weight)
        {
            int centerIndex = _vertices.Count;
            _vertices.Add(tip);
            _normals.Add((tip - center).normalized);
            _uvs.Add(new Vector2(0.5f, 0.5f));
            _weights.Add(weight);

            for (int side = 0; side < sides; side++)
            {
                int nextSide = (side + 1) % sides;
                AddTriangle(centerIndex, ringStart + side, ringStart + nextSide);
            }
        }

        private static Vector3 RingDirection(IReadOnlyList<Vector3> spine, int ring)
        {
            if (ring == 0) return (spine[1] - spine[0]).normalized;
            if (ring == spine.Count - 1) return (spine[ring] - spine[ring - 1]).normalized;
            return ((spine[ring + 1] - spine[ring]).normalized + (spine[ring] - spine[ring - 1]).normalized).normalized;
        }
    }
}
