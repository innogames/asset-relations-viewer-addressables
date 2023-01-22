using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Com.Innogames.Core.Frontend.NodeDependencyLookup.Addressables
{
	public class AssetToAddressableGroupDependency
	{
		public const string Name = "AssetToAddressableGroup";
	}

	public class AddressableAssetToGroupTempCache : IDependencyCache
	{
		private const string Version = "2.0.0";
		private const string FileName = "AssetToAddressableGroupDependencyCacheData_" + Version + ".cache";

		private GenericDependencyMappingNode[] Nodes = new GenericDependencyMappingNode[0];
		private Dictionary<string, GenericDependencyMappingNode> Lookup = new Dictionary<string, GenericDependencyMappingNode>();
		private CreatedDependencyCache _createdDependencyCache;

		public void ClearFile(string directory)
		{
		}

		public void Initialize(CreatedDependencyCache createdDependencyCache)
		{
			_createdDependencyCache = createdDependencyCache;
		}

		public bool CanUpdate()
		{
			return !Application.isPlaying;
		}

		private RelationLookup.RelationsLookup GetAssetToFileLookup(CacheUpdateInfo updateInfo)
		{
			NodeDependencyLookupContext context = new NodeDependencyLookupContext();
			ResolverUsageDefinitionList resolverList = new ResolverUsageDefinitionList();
			resolverList.Add<AssetToFileDependencyCache, AssetToFileDependencyResolver>(updateInfo.Load, updateInfo.Update, updateInfo.Save);
			NodeDependencyLookupUtility.LoadDependencyLookupForCaches(context, resolverList);

			return context.RelationsLookup;
		}

		public bool Update(ResolverUsageDefinitionList resolverUsages, bool shouldUpdate)
		{
			if(!shouldUpdate && Nodes.Length > 0)
			{
				return false;
			}

			CacheUpdateInfo resolverUpdateInfo = resolverUsages.GetUpdateStateForResolver(typeof(AddressableAssetGroupResolver));
			RelationLookup.RelationsLookup assetToFileLookup = GetAssetToFileLookup(resolverUpdateInfo);
			AddressableAssetSettings settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;

			if (settings == null)
			{
				return false;
			}

			Lookup.Clear();
			Nodes = new GenericDependencyMappingNode[0];

			List<GenericDependencyMappingNode> nodes = new List<GenericDependencyMappingNode>();

			for (int i = 0; i < settings.groups.Count; ++i)
			{
				AddressableAssetGroup group = settings.groups[i];
				EditorUtility.DisplayProgressBar("AddressableAssetToGroupTempCache", $"Getting dependencies for {group.Name}", i / (float)(settings.groups.Count));

				foreach (AddressableAssetEntry addressableAssetEntry in group.entries)
				{
					List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>();
					addressableAssetEntry.GatherAllAssets(entries, true, true, false);

					foreach (AddressableAssetEntry assetEntry in entries)
					{
						Node fileNode = assetToFileLookup.GetNode(assetEntry.guid, FileNodeType.Name);

						if (fileNode == null)
						{
							continue;
						}

						string assetId = fileNode.Referencers[0].Node.Id;
						GenericDependencyMappingNode node = new GenericDependencyMappingNode(assetId, AssetNodeType.Name);

						node.Dependencies.Add(new Dependency(group.Name, AssetToAddressableGroupDependency.Name, AddressableAssetGroupNodeType.Name, new []{new PathSegment("AssetGroup", PathSegmentType.Property), }));

						nodes.Add(node);
						Lookup.Add(node.Id, node);
					}
				}
			}

			Nodes = nodes.ToArray();
			return true;
		}

		public void AddExistingNodes(List<IDependencyMappingNode> nodes)
		{
			foreach (IDependencyMappingNode node in Nodes)
			{
				nodes.Add(node);
			}
		}

		public string GetHandledNodeType()
		{
			return "Asset";
		}

		public List<Dependency> GetDependenciesForId(string id)
		{
			if(NodeDependencyLookupUtility.IsResolverActive(_createdDependencyCache, AddressableAssetToGroupResolver.Id, AssetToAddressableGroupDependency.Name) && Lookup.ContainsKey(id))
			{
				return Lookup[id].Dependencies;
			}

			return new List<Dependency>();
		}

		public void Load(string directory)
		{
			string path = Path.Combine(directory, FileName);

			Nodes = CacheSerializerUtils.LoadGenericLookup(path);
			Lookup = CacheSerializerUtils.GenerateIdLookup(Nodes);
		}

		public void Save(string directory)
		{
			CacheSerializerUtils.SaveGenericMapping(directory, FileName, Nodes);
		}

		public void InitLookup()
		{
		}

		public Type GetResolverType()
		{
			return typeof(IAddressableAssetToGroupResolver);
		}
	}

	public interface IAddressableAssetToGroupResolver : IDependencyResolver
	{
	}

	public class AddressableAssetToGroupResolver : IAddressableAssetToGroupResolver
	{
		public const string Id = "AddressableAssetToGroupResolver";

		private string[] ConnectionTypes = {AssetToAddressableGroupDependency.Name};
		private const string ConnectionTypeDescription = "Dependencies from the asset to the AddressableAssetGroup the asset is part of";
		private static DependencyType DependencyType = new DependencyType("Asset->AddressableAssetGroup", new Color(0.85f, 0.55f, 0.35f), false, true, ConnectionTypeDescription);

		public string[] GetDependencyTypes()
		{
			return ConnectionTypes;
		}

		public string GetId()
		{
			return Id;
		}

		public DependencyType GetDependencyTypeForId(string typeId)
		{
			return DependencyType;
		}
	}
}