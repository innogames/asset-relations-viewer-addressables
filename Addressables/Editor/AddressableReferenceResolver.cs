using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Com.Innogames.Core.Frontend.NodeDependencyLookup.Addressables
{
	/// <summary>
	/// Resolver to find dependencies to assets which are connected via the AddressableAssets system
	/// </summary>
	public class AddressableReferenceResolver : IAssetDependencyResolver
	{
		private static ConnectionType AddressableType = new ConnectionType(new Color(0.6f, 0.7f, 0.85f), true, false);

		private readonly HashSet<string> validGuids = new HashSet<string>();
		
		public const string ResolvedType = "Addressable";
		public const string Id = "AddressableReferenceResolver";
		
		private AddressableSerializedPropertyTraverserSubSystem SubSystem = new AddressableSerializedPropertyTraverserSubSystem();
		
		public void GetDependenciesForId(string guid, List<Dependency> dependencies)
		{
			HashSet<string> foundDependenciesHashSet = new HashSet<string>();

			if (SubSystem.Dependencies.ContainsKey(guid))
			{
				foreach (Dependency dependency in SubSystem.Dependencies[guid])
				{
					string dependencyGuid = dependency.Id;

					if (dependencyGuid != guid)
					{
						dependencies.Add(dependency);
						foundDependenciesHashSet.Add(dependencyGuid);
					}
				}
			}
		}

		public bool IsGuidValid(string guid)
		{
			return true;
		}

		public string GetId()
		{
			return Id;
		}

		public ConnectionType GetDependencyTypeForId(string typeId)
		{
			return AddressableType;
		}

		public string[] GetConnectionTypes()
		{
			return new[] { ResolvedType };
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

		public void Initialize(AssetDependencyCache cache, HashSet<string> changedAssets, ProgressBase progress)
		{
			SubSystem.Clear();

			foreach (string guid in changedAssets)
			{
				if (validGuids.Contains(guid))
				{
					cache._hierarchyTraverser.AddGuid(guid, SubSystem);
				}
			}
		}
	}

	public class AddressableSerializedPropertyTraverserSubSystem : SerializedPropertyTraverserSubSystem
	{
		public static readonly string ResolvedTypeAddressable = "Addressable";
		public static readonly string NodeType = "Asset";

		private Type AddressableType = typeof(AssetReference);

		public override void TraversePrefab(string id, UnityEngine.Object obj, Stack<PathSegment> stack)
		{
		}

		public override Result GetDependency(Type objType, object obj, SerializedProperty property, string propertyPath, SerializedPropertyType type, Stack<PathSegment> stack)
		{
			if (obj != null && IsType(obj.GetType(), AddressableType))
			{
				AssetReference assetReference = obj as AssetReference;

				if (assetReference != null && assetReference.editorAsset != null)
				{
					string path = AssetDatabase.GetAssetPath(assetReference.editorAsset);
					string guid = AssetDatabase.AssetPathToGUID(path);
					return new Result{Id = guid, NodeType = NodeType, ConnectionType = ResolvedTypeAddressable};
				}
			}
			
			return null;
		}
		
		private bool IsType(Type type, Type requiredType)
		{
			string fullName = type.FullName;
			string requeredTypeFullName = requiredType.FullName;

			if (fullName == requeredTypeFullName)
			{
				return true;
			}

			if (type.BaseType != null)
			{
				return IsType(type.BaseType, requiredType);
			}

			return false;
		}
	}
}