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

		private MethodInfo subObjectMethodInfo;

		public readonly Dictionary<string, List<Dependency>> Dependencies = new Dictionary<string, List<Dependency>>();

		public void GetDependenciesForId(string assetId, List<Dependency> dependencies)
		{
			if (Dependencies.ContainsKey(assetId))
			{
				foreach (Dependency dependency in Dependencies[assetId])
				{
					string dependencyGuid = dependency.Id;

					if (dependencyGuid != assetId)
					{
						dependencies.Add(dependency);
					}
				}
			}
		}

		public bool IsGuidValid(string guid)
		{
			return validGuids.Contains(guid);
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

		public void Initialize(AssetDependencyCache cache)
		{
			Dependencies.Clear();

			Type assetReferenceType = typeof(AssetReference);
			subObjectMethodInfo = assetReferenceType.GetMethod("get_SubOjbectType", BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);

			if (subObjectMethodInfo == null)
			{
				// Try version without typo
				subObjectMethodInfo = assetReferenceType.GetMethod("get_SubObjectType", BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
			}
		}

		public void TraversePrefab(string id, UnityEngine.Object obj, Stack<PathSegment> stack)
		{
			// No implementation
		}

		public void TraversePrefabVariant(string id, Object obj, Stack<PathSegment> stack)
		{
			// No implementation
		}

		public void AddDependency(string id, Dependency dependency)
		{
			if (!Dependencies.ContainsKey(id))
			{
				Dependencies.Add(id, new List<Dependency>());
			}

			Dependencies[id].Add(dependency);
		}

		public AssetDependencyResolverResult GetDependency(string sourceAssetId, object obj, string propertyPath, SerializedPropertyType type)
		{
			if (obj is AssetReference assetReference && assetReference.editorAsset != null)
			{
				Object asset = assetReference.editorAsset;

				if (assetReference.SubObjectName != null && subObjectMethodInfo != null)
				{
					Type subObjectType = subObjectMethodInfo.Invoke(assetReference, new object[] {}) as Type;

					Object[] allAssets = null;

					if (asset is SceneAsset)
					{
						allAssets = new Object[] {asset};
					}
					else
					{
						allAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(assetReference.AssetGUID));
					}

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
				return new AssetDependencyResolverResult{Id = assetId, NodeType = AssetNodeType.Name, DependencyType = AssetToAssetAssetRefDependency.Name};
			}

			return null;
		}
	}
}