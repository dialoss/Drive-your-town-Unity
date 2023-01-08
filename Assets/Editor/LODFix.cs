/**
* Copyright (c) 2018 Hrvoje Jukic
*
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
* DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
* OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE
* OR OTHER DEALINGS IN THE SOFTWARE. 
*/
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extension methods for detecting and removing <see cref="Renderer"/>s occuring in multiple <see cref="LOD"/>s of distinct <see cref="LODGroup"/>s.
/// </summary>
public static class LODFix
{

    /// <summary>
    /// Tracks which <see cref="Renderer"/>s should be removed from a <see cref="LOD"/> and removes creates a new one by invoking <see cref="getLOD"/>.
    /// </summary>
    private struct PendingLODFix
    {
        /// <summary>
        /// Tracked <see cref="LOD"/>.
        /// </summary>
        private LOD lod;
        /// <summary>
        /// Indicates whether the corresponding <see cref="LOD.renderers"/> of <see cref="lod"/> should remain present in it.
        /// </summary>
        private bool[] shouldBeKept;
        /// <summary>
        /// Number of <see cref="Renderer"/>s whose removal from <see cref="lod"/> was requested.
        /// </summary>
        private int removedRendererCount;

        /// <summary>
        /// Start tracking which <see cref="Renderer"/>s should be removed from <paramref name="lod"/>.
        /// </summary>
        /// <param name="lod">Tracked <see cref="LOD"/>.</param>
        public PendingLODFix(LOD lod)
        {
            this.lod = lod;
            // Track each renderer.
            shouldBeKept = new bool[lod.renderers.Length];
            for (int rendererIndex = 0; rendererIndex < shouldBeKept.Length; rendererIndex++)
            {
                shouldBeKept[rendererIndex] = true;
            }
            // No removals so far.
            removedRendererCount = 0;
        }
        
        /// <summary>
        /// Requests removal of <paramref name="renderer"/> from <see cref="LOD.renderers"/> in <see cref="lod"/>.
        /// </summary>
        /// <param name="renderer">Renderer to be removed from <see cref="lod"/>, if present.</param>
        /// <returns>True when <paramref name="renderer"/> occurs in <see cref="LOD.renderers"/> of <see cref="lod"/>.</returns>
        public bool Remove(Renderer renderer)
        {
            bool found = false;
            // Check match against each renderer.
            for (int rendererIndex = 0; rendererIndex < shouldBeKept.Length; rendererIndex++)
            {
                // Check if new match is found.
                if(shouldBeKept[rendererIndex] && lod.renderers[rendererIndex] == renderer)
                {
                    shouldBeKept[rendererIndex] = false;
                    found = true;
                    removedRendererCount++;
                }
            }
            return found;
        }

        /// <summary>
        /// Generates a new <see cref="LOD"/> from <see cref="lod"/> without <see cref="Renderer"/>s whose removal was requested by using <see cref="Remove(Renderer)"/>.
        /// </summary>
        /// <returns><see cref="LOD"/> without undesired <see cref="Renderer"/>s.</returns>
        /// <remarks>Must not be called multiple times.</remarks>
        public LOD getLOD()
        {
            // Optimisation: Avoid allocating new array if not neccessary.
            if (removedRendererCount > 0)
            {
                Debug.Log("Removing " + removedRendererCount + " from LOD.");

                // Create new array of renderers wihtout undesired ones.
                int newArrayIndex = 0;
                Renderer[] newArray = new Renderer[lod.renderers.Length - removedRendererCount];
                for (int rendererIndex = 0; rendererIndex < lod.renderers.Length; rendererIndex++)
                {
                    if (shouldBeKept[rendererIndex])
                    {
                        newArray[newArrayIndex++] = lod.renderers[rendererIndex];
                    }
                }

                // Verify that removedRendererCount properly tracked the number of removed renderers.
                Debug.Assert(newArrayIndex == newArray.Length);

                // Apply changes.
                lod.renderers = newArray;
            }
            return lod;
        }
    }

    /// <summary>
    /// Tracks which <see cref="Renderer"/>s should be removed from <see cref="LOD"/>s of a <see cref="LODGroup"/> and removes them all at once when inovking <see cref="ApplyFix"/>.
    /// Each change in the <see cref="LODGroup"/> causes it to reclculate, which is slow and consumes plenty of memory.
    /// </summary>
    private class PendingLODGroupFix
    {
        /// <summary>
        /// Tracked LOD group.
        /// </summary>
        private LODGroup lodGroup;
        /// <summary>
        /// Tracked changes to each <see cref="LOD"/> in <see cref="lodGroup"/>.
        /// </summary>
        private PendingLODFix[] pendingLODFixes;
        /// <summary>
        /// Inidates whether removal of a <see cref="Renderer"/> which is present int <see cref="lodGroup"/> was requested.
        /// </summary>
        private bool shouldSwapLODs;

        /// <summary>
        /// Start tracking which <see cref="Renderer"/>s should be removed from <paramref name="lodGroup"/>.
        /// </summary>
        /// <param name="lodGroup">Tracked LOD group.</param>
        public PendingLODGroupFix(LODGroup lodGroup)
        {
            this.lodGroup = lodGroup;

            // Track changes on each LOD.
            var lods = lodGroup.GetLODs();
            pendingLODFixes = new PendingLODFix[lods.Length];
            for (int lodIndex = 0; lodIndex < lods.Length; lodIndex++)
            {
                pendingLODFixes[lodIndex] = new PendingLODFix(lods[lodIndex]);
            }

            // No need to change the LOD group if no renderers get removed.
            shouldSwapLODs = false;
        }

        /// <summary>
        /// Requests removal of <paramref name="renderer"/> from all <see cref="LOD"/>s in <see cref="lodGroup"/>.
        /// </summary>
        /// <param name="renderer">Renderer to be removed from <see cref="lodGroup"/>, if present.</param>
        /// <returns>True when <paramref name="renderer"/> occurs in <see cref="lodGroup"/>.</returns>
        /// <remarks>All accumulated removal requests are applied when invoking <see cref="ApplyFix"/></remarks>
        public bool Remove(Renderer renderer)
        {
            bool found = false;
            // Remove renderer from each lod.
            for (int lodIndex = 0; lodIndex < pendingLODFixes.Length; lodIndex++)
            {
                found |= pendingLODFixes[lodIndex].Remove(renderer);
                shouldSwapLODs |= found;
            }
            return found;
        }

        /// <summary>
        /// Applies all removal requests issues using <see cref="Remove(Renderer)"/>
        /// </summary>
        /// <remarks>Must not be invoked multiple times.</remarks>
        public void ApplyFix()
        {
            // Optimisation: Avoid changing the LODs if not neccessary to avoid recalculations.
            if (shouldSwapLODs)
            {
                // Compose new LODs without the renderers to be removed.
                LOD[] lods = new LOD[pendingLODFixes.Length];
                for (int lodIndex = 0; lodIndex < pendingLODFixes.Length; lodIndex++)
                {
                    lods[lodIndex] = pendingLODFixes[lodIndex].getLOD();
                }
                // Set LODs without undesired renderers.
                lodGroup.SetLODs(lods);
            }
        }
    }

    /// <summary>
    /// Ensures that <see cref="Renderer"/>s in <see cref="LOD"/>s in <see cref="LODGroup"/>s are not shared with ancestors by giving priority to the deeper nested ones.
    /// </summary>
    /// <param name="root">Transform inside which some <see cref="LODGroup"/>s might reference the same <see cref="Renderer"/>s.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="root"/> is null.</exception>
    public static void FixLOD(this Transform root)
    {
        // Arugment validation.
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }
        Stack<PendingLODGroupFix> emptyStack = new Stack<PendingLODGroupFix>();
        FixRecursively(root, emptyStack);
        //Debug.Log("LODs fixed for " + root.name);
    }

    /// <summary>
    /// Removes <see cref="Renderer"/>s from <see cref="LODGroup"/>s if they are used by a nested <see cref="LODGroup"/>.
    /// </summary>
    /// <param name="nestedTransform">Transform inside which some <see cref="LODGroup"/>s might have intersecting <see cref="Renderer"/>s in between or with <paramref name="lodGroupsOfAncestors"/>.</param>
    /// <param name="lodGroupsOfAncestors">The <see cref="LODGroup"/>s of <paramref name="nestedTransform"/>'s ancestors.</param>
    private static void FixRecursively(Transform nestedTransform, Stack<PendingLODGroupFix> lodGroupsOfAncestors)
    {
        int lodgroupCount = lodGroupsOfAncestors.Count;
        var localLodGroups = nestedTransform.GetComponents<LODGroup>();

        foreach (var localLodGroup in localLodGroups)
        {
            // Remove intersecting renderers from already processed LOD groups.
            foreach (var localLod in localLodGroup.GetLODs())
            {
                foreach (var locallyUsedRenderer in localLod.renderers)
                {
                    foreach (var ancestorLodGroup in lodGroupsOfAncestors)
                    {
                        ancestorLodGroup.Remove(locallyUsedRenderer);
                    }
                }
            }
            
            // Add local LOD group to already processed LOD groups.
            lodGroupsOfAncestors.Push(new PendingLODGroupFix(localLodGroup));
        }

        // Fix LODs in children with local LOD groups already added to lodGroupsOfAncestors.
        foreach (Transform child in nestedTransform)
        {
            FixRecursively(child, lodGroupsOfAncestors);
        }

        // Remove local LOD groups from lodGroupsOfAncestors before finishing.
        for (int i = 0; i < localLodGroups.Length; i++)
        {
            lodGroupsOfAncestors.Pop().ApplyFix();
        }

        // Ensure that all descendats and itself properly removed their lod groups.
        Debug.Assert(lodgroupCount == lodGroupsOfAncestors.Count);
    }
}
