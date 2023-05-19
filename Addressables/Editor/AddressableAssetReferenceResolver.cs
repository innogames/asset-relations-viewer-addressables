using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace Com.Innogames.Core.Frontend.NodeDependencyLookup.Addressables
{
	public class AssetToAssetAssetRefDependency
	{
		public const string Name = "ATOA_AssetRef";
	}

	/// <summary>
	/// Resolver to find dependencies to assets which are connected via the AddressableAssets system
	/// </summary>
	public class AddressableAssetReferenceResolver : IAssetDependencyResolver
	{
		private const string ConnectionTypeDescription = "Dependencies between assets done by an Addressable AssetReference";
		private static DependencyType AddressableType = new DependencyType("Asset->Asset by AssetReference", new Color(0.6f, 0.7f, 0.85f), true, false, ConnectionTypeDescription);

		private readonly HashSet<string> validGuids = new HashSet<string>();
		private const string Id = "AddressableReferenceResolver";

		private AddressableSerializedPropertyTraverserSubSystem TraverserSubSystem = new AddressableSerializedPropertyTraverserSubSystem();

		public void GetDependenciesForId(string assetId, List<Dependency> dependencies)
		{
			TraverserSubSystem.GetDependenciesForId(assetId, dependencies);
		}

		public bool IsGuidValid(string guid)
		{
			return true;
		}

		public string GetId()
		{
			return Id;
		}

		public DependencyType GetDependencyTypeForId(string typeId)
		{
			return AddressableType;
		}

		public string[] GetDependencyTypes()
		{
			return new[] { AssetToAssetAssetRefDependency.Name };
		}

		public void SetValidGUIDs()
		{
			validGuids.Clear();

			foreach (string guid in AssetDatabase.FindAssets("t:GameObject"))
			{
				validGuids.Add(guid);
			}
			foreach (string guid in AssetDatabase.FindAssets("t:Scene"))
			{
				validGuids.Add(guid);
			}
			foreach (string guid in AssetDatabase.FindAssets("t:Material"))
			{
				validGuids.Add(guid);
			}
			foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject"))
			{
				validGuids.Add(guid);
			}
		}

		public void Initialize(AssetDependencyCache cache, HashSet<string> changedAssets)
		{
			TraverserSubSystem.Clear();

			foreach (string assetId in changedAssets)
			{
				string guid = NodeDependencyLookupUtility.GetGuidFromAssetId(assetId);
				if (validGuids.Contains(guid))
				{
					cache._hierarchyTraverser.AddAssetId(assetId, TraverserSubSystem);
				}
			}
		}
	}

	public class AddressableSerializedPropertyTraverserSubSystem : SerializedPropertyTraverserSubSystem
	{
		private MethodInfo subObjectMethodInfo;

		public AddressableSerializedPropertyTraverserSubSystem()
		{
			Type assetReferenceType = typeof(AssetReference);
			subObjectMethodInfo = assetReferenceType.GetMethod("get_SubOjbectType", BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

			if (subObjectMethodInfo == null)
			{
				// Try version without typo
				subObjectMethodInfo = assetReferenceType.GetMethod("get_SubObjectType", BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
			}
		}

		public override void TraversePrefab(string id, UnityEngine.Object obj, Stack<PathSegment> stack)
		{
			// No implementation
		}

		public override void TraversePrefabVariant(string id, Object obj, Stack<PathSegment> stack)
		{
			// No implementation
		}

		public override Result GetDependency(string sourceAssetId, object obj, string propertyPath, SerializedPropertyType type)
		{
			if (obj is AssetReference assetReference && assetReference.editorAsset != null)
			{
				Object asset = assetReference.editorAsset;

				if (assetReference.SubObjectName != null && subObjectMethodInfo != null)
				{
					Type subObjectType = subObjectMethodInfo.Invoke(assetReference, new object[] {}) as Type;
					Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(assetReference.AssetGUID));

					foreach (Object allAsset in allAssets)
					{
						if (allAsset.name == assetReference.SubObjectName && allAsset.GetType() == subObjectType)
						{
							asset = allAsset;
							break;
						}
					}
				}

				string assetId = NodeDependencyLookupUtility.GetAssetIdForAsset(asset);
					return new Result{Id = assetId, NodeType = AssetNodeType.Name, DependencyType = AssetToAssetAssetRefDependency.Name};
			}

			return null;
		}
	}
}