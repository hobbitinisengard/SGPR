using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshCombiner
{
    private const int Mesh16BitBufferVertexLimit = 65535;

    [SerializeField]
    private bool createMultiMaterialMesh = false, combineInactiveChildren = false, deactivateCombinedChildren = true,
        deactivateCombinedChildrenMeshRenderers = false, generateUVMap = false, destroyCombinedChildren = false;
    [SerializeField]
    private string folderPath = "Prefabs/CombinedMeshes";
    [SerializeField]
    [Tooltip("MeshFilters with Meshes which we don't want to combine into one Mesh.")]
    private MeshFilter[] meshFiltersToSkip = null;

    public bool CreateMultiMaterialMesh { get { return createMultiMaterialMesh; } set { createMultiMaterialMesh = value; } }
    public bool CombineInactiveChildren { get { return combineInactiveChildren; } set { combineInactiveChildren = value; } }
    public bool DeactivateCombinedChildren
    {
        get { return deactivateCombinedChildren; }
        set
        {
            deactivateCombinedChildren = value;
            CheckDeactivateCombinedChildren();
        }
    }
    public bool DeactivateCombinedChildrenMeshRenderers
    {
        get { return deactivateCombinedChildrenMeshRenderers; }
        set
        {
            deactivateCombinedChildrenMeshRenderers = value;
            CheckDeactivateCombinedChildren();
        }
    }
    public bool GenerateUVMap { get { return generateUVMap; } set { generateUVMap = value; } }
    public bool DestroyCombinedChildren
    {
        get { return destroyCombinedChildren; }
        set
        {
            destroyCombinedChildren = value;
            CheckDestroyCombinedChildren();
        }
    }
    public string FolderPath { get { return folderPath; } set { folderPath = value; } }


    private void CheckDeactivateCombinedChildren()
    {
        if (deactivateCombinedChildren || deactivateCombinedChildrenMeshRenderers)
        {
            destroyCombinedChildren = false;
        }
    }

    private void CheckDestroyCombinedChildren()
    {
        if (destroyCombinedChildren)
        {
            deactivateCombinedChildren = false;
            deactivateCombinedChildrenMeshRenderers = false;
        }
    }



   
    public Mesh CombineObjects(List<GameObject> objectsToCombine, bool showCreatedMeshInfo = false)
    {
        #region Get MeshFilters, MeshRenderers and unique Materials from all children:
        MeshFilter[] meshFilters = new MeshFilter[objectsToCombine.Count];
        for(int i = 0; i < objectsToCombine.Count; i++)
        {
            meshFilters[i] = objectsToCombine[i].GetComponent<MeshFilter>();
        }
        MeshRenderer[] meshRenderers = new MeshRenderer[meshFilters.Length];

        List<Material> uniqueMaterialsList = new List<Material>();
        for (int i = 0; i < meshFilters.Length; i++)
        {
            meshRenderers[i] = meshFilters[i].GetComponent<MeshRenderer>();
            if (meshRenderers[i] != null)
            {
                Material[] materials = meshRenderers[i].sharedMaterials; // Get all Materials from child Mesh.
                for (int j = 0; j < materials.Length; j++)
                {
                    if (!uniqueMaterialsList.Contains(materials[j])) // If Material doesn't exists in the list then add it.
                    {
                        uniqueMaterialsList.Add(materials[j]);
                    }
                }
            }
        }
        #endregion Get MeshFilters, MeshRenderers and unique Materials from all children.

        #region Combine children Meshes with the same Material to create submeshes for final Mesh:
        List<CombineInstance> finalMeshCombineInstancesList = new List<CombineInstance>();

        // If it will be over 65535 then use the 32 bit index buffer:
        long verticesLength = 0;

        for (int i = 0; i < uniqueMaterialsList.Count; i++) // Create each Mesh (submesh) from Meshes with the same Material.
        {
            List<CombineInstance> submeshCombineInstancesList = new List<CombineInstance>();

            for (int j = 0; j < meshFilters.Length; j++) // Get only childeren Meshes (skip our Mesh).
            {
                if (meshRenderers[j] != null)
                {
                    Material[] submeshMaterials = meshRenderers[j].sharedMaterials; // Get all Materials from child Mesh.

                    for (int k = 0; k < submeshMaterials.Length; k++)
                    {
                        // If Materials are equal, combine Mesh from this child:
                        if (uniqueMaterialsList[i] == submeshMaterials[k])
                        {
                            CombineInstance combineInstance = new CombineInstance();
                            combineInstance.subMeshIndex = k; // Mesh may consist of smaller parts - submeshes.
                                                              // Every part have different index. If there are 3 submeshes
                                                              // in Mesh then MeshRender needs 3 Materials to render them.
                            combineInstance.mesh = meshFilters[j].sharedMesh;
                            combineInstance.transform = meshFilters[j].transform.localToWorldMatrix;
                            submeshCombineInstancesList.Add(combineInstance);
                            verticesLength += combineInstance.mesh.vertices.Length;
                        }
                    }
                }
            }

            // Create new Mesh (submesh) from Meshes with the same Material:
            Mesh submesh = new Mesh();

#if UNITY_2017_3_OR_NEWER
            if (verticesLength > Mesh16BitBufferVertexLimit)
            {
                submesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Only works on Unity 2017.3 or higher.
            }

            submesh.CombineMeshes(submeshCombineInstancesList.ToArray(), true);
#else
			// Below Unity 2017.3 if vertices count is above the limit then an error appears in the console when we use the below method.
			// Anyway we don't stop the algorithm here beacuse we want to count the entire number of vertices in the children meshes:
			if(verticesLength <= Mesh16BitBufferVertexLimit)
			{
				submesh.CombineMeshes(submeshCombineInstancesList.ToArray(), true);
			}
#endif

            CombineInstance finalCombineInstance = new CombineInstance();
            finalCombineInstance.subMeshIndex = 0;
            finalCombineInstance.mesh = submesh;
            finalCombineInstance.transform = Matrix4x4.identity;
            finalMeshCombineInstancesList.Add(finalCombineInstance);
        }
        #endregion Combine submeshes (children Meshes) with the same Material.

        #region Set Materials array & combine submeshes into one multimaterial Mesh:
        Mesh combinedMesh = new Mesh();

#if UNITY_2017_3_OR_NEWER
        if (verticesLength > Mesh16BitBufferVertexLimit)
        {
            combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Only works on Unity 2017.3 or higher.
        }

        combinedMesh.CombineMeshes(finalMeshCombineInstancesList.ToArray(), false);
        //DeactivateCombinedGameObjects(meshFilters);

        //if (showCreatedMeshInfo)
        //{
        //    if (verticesLength <= Mesh16BitBufferVertexLimit)
        //    {
        //        Debug.Log("<color=#00cc00><b>Mesh \"" + name + "\" was created from " + (meshFilters.Length - 1) + " children meshes and has "
        //            + finalMeshCombineInstancesList.Count + " submeshes, and " + verticesLength + " vertices.</b></color>");
        //    }
        //    else
        //    {
        //        Debug.Log("<color=#ff3300><b>Mesh \"" + name + "\" was created from " + (meshFilters.Length - 1) + " children meshes and has "
        //            + finalMeshCombineInstancesList.Count + " submeshes, and " + verticesLength
        //            + " vertices. Some old devices, like Android with Mali-400 GPU, do not support over 65535 vertices.</b></color>");
        //    }
        //}
#else
		if(verticesLength <= Mesh16BitBufferVertexLimit)
		{
			combinedMesh.CombineMeshes(finalMeshCombineInstancesList.ToArray(), false);
			GenerateUV(combinedMesh);
			meshFilters[0].sharedMesh = combinedMesh;
			DeactivateCombinedGameObjects(meshFilters);

			if(showCreatedMeshInfo)
			{
				Debug.Log("<color=#00cc00><b>Mesh \""+name+"\" was created from "+(meshFilters.Length-1)+" children meshes and has "
					+finalMeshCombineInstancesList.Count+" submeshes, and "+verticesLength+" vertices.</b></color>");
			}
		}
		else if(showCreatedMeshInfo)
		{
			Debug.Log("<color=red><b>The mesh vertex limit is 65535! The created mesh had "+verticesLength+" vertices. Upgrade Unity version to"
				+" 2017.3 or higher to avoid this limit (some old devices, like Android with Mali-400 GPU, do not support over 65535 vertices).</b></color>");
		}
#endif
        #endregion Set Materials array & combine submeshes into one multimaterial Mesh.

        combinedMesh = Info.MergeVertices(combinedMesh);
        return combinedMesh;
    }
}
