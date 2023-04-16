using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain.DoubleMath;

namespace PlanetaryTerrain
{
    public class QuadPool
    {
        private Planet planet;
        private List<Quad> quadsPool = new List<Quad>();
        private const int quadPoolMaxSize = 30;

        /// <summary>
        /// Returns a Quad, either from the pool if possible or instantiated
        /// </summary>
        internal Quad GetQuad(Vector3 trPosition, Quaternion rotation)
        {
            if (quadsPool.Count == 0)
                return new Quad(trPosition, rotation);
            else
            {
                Quad q = quadsPool[0];
                quadsPool.Remove(q);
                q.trPosition = trPosition;
                q.rotation = rotation;
                return q;
            }
        }

        /// <summary>
        /// Removes Quad, either moves it to the pool or destroys it
        /// </summary>
        internal void RemoveQuad(Quad quad)
        {
            if (quad.meshGenerator != null && quad.meshGenerator.isRunning)
            {
                quad.meshGenerator.Dispose();
                quad.meshGenerator = null;
            }
            if (quad.coroutine != null)
            {
                if (!quad.isSplitting) //There are two possible coroutines: Splitting and generating foliage/detailObjects. If there is a coroutine, and the quad is not splitting, it is currently generating detail objects.
                    planet.detailObjectsGenerating--;
                planet.StopCoroutine(quad.coroutine);
            }
            if (quad.meshGenerator != null)
            {
                quad.meshGenerator.Dispose();
                quad.meshGenerator = null;
            }

            if (quad.children != null)
                for (int i = 0; i < quad.children.Length; i++)
                    RemoveQuad(quad.children[i]);

            if (quad.renderedQuad)
                planet.quadGameObjectPool.RemoveGameObject(quad);

            MonoBehaviour.Destroy(quad.mesh);
            quad.Reset();
            planet.quads.Remove(quad);
            planet.quadSplitQueue.Remove(quad);
            planet.quadIndicies.Remove(quad.index);

            if (quadsPool.Count < quadPoolMaxSize)
                quadsPool.Add(quad);
        }

        public QuadPool(Planet planet)
        {
            this.planet = planet;
        }
    }

    public class QuadGameObjectPool
    {
        private Planet planet;
        private List<GameObject> quadGOPool = new List<GameObject>();
        private const int renderedQuadPoolMaxSize = 30;
        /// <summary>
        /// Adds renderedQuad to a Quad, either from pool or instantiated.
        /// </summary>
        internal GameObject GetGameObject(Quad quad)
        {
            GameObject rquad = null;

            QuaternionD rotation = planet.rotation;
            Vector3d plPos = planet.transform.position.ToVector3d();

            Vector3 position = planet.GetRenderedQuadPosition(quad);

            if (quadGOPool.Count == 0)
            {
                rquad = (GameObject)MonoBehaviour.Instantiate(planet.quadGO, position, rotation);
                rquad.GetComponent<MeshRenderer>().material = planet.planetMaterial;
                if (planet.hideQuads)
                    rquad.hideFlags = HideFlags.HideInHierarchy;
            }
            else
            {
                rquad = quadGOPool[quadGOPool.Count - 1];
                quadGOPool.RemoveAt(quadGOPool.Count - 1);

                if (rquad == null)
                {
                    rquad = (GameObject)MonoBehaviour.Instantiate(planet.quadGO, position, rotation);
                    rquad.GetComponent<MeshRenderer>().material = planet.planetMaterial;
                    if (planet.hideQuads)
                        rquad.hideFlags = HideFlags.HideInHierarchy;
                }

                rquad.transform.position = position;
                rquad.transform.rotation = rotation;
            }
            rquad.GetComponent<MeshFilter>().mesh = quad.mesh;
            rquad.name = "Quad " + quad.index;

            if (planet.generateColliders[quad.level])
                rquad.AddComponent<MeshCollider>().convex = false;

                
            return rquad;
        }

        /// <summary>
        /// Removes renderedQuad, either moves it to pool or destroys it
        /// </summary>
        internal void RemoveGameObject(Quad quad)
        {
            if (quadGOPool.Count < renderedQuadPoolMaxSize)
            {
                MonoBehaviour.Destroy(quad.renderedQuad.GetComponent<PlanetaryTerrain.Foliage.FoliageRenderer>());
                MonoBehaviour.Destroy(quad.renderedQuad.GetComponent<MeshCollider>());
                quadGOPool.Add(quad.renderedQuad);
                quad.renderedQuad.SetActive(false);
                quad.renderedQuad = null;
            }
            else
            {
                MonoBehaviour.Destroy(quad.renderedQuad);
            }
        }

        public QuadGameObjectPool(Planet planet)
        {
            this.planet = planet;
        }
    }
}