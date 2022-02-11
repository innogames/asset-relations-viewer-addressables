using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Com.Innogames.Core.Frontend.NodeDependencyLookup.Addressables
{
	public class AddressableGroupNodeType
	{
		public const string Name = "AddressableGroup";
	}
	
	public class AddressableAssetGroupDependencyNode : IResolvedNode
	{
		public string groupId;

		public string Id{get { return groupId; }}
		public string Type { get { return AddressableGroupNodeType.Name; }}
		public bool Existing {get { return true; }}
		
		public List<Dependency> Dependencies = new List<Dependency>();
	}
	
	public class AddressableAssetGroupTempCache : IDependencyCache
	{
		private IResolvedNode[] Nodes = new IResolvedNode[0];
		private Dictionary<string, AddressableAssetGroupDependencyNode> Lookup = new Dictionary<string, AddressableAssetGroupDependencyNode>();
		public const string ConnectionType = "AddressableGroupUsage";
		
		private CreatedDependencyCache _createdDependencyCache;
		
		public void ClearFile(string directory)
		{
		}

		public void Initialize(CreatedDependencyCache createdDependencyCache)
		{
			_createdDependencyCache = createdDependencyCache;
		}

		public bool NeedsUpdate(ProgressBase progress)
		{
			return true;
		}

		public bool CanUpdate()
		{
			return !Application.isPlaying;
		}

		public void Update(ProgressBase progress)
		{
			AddressableAssetSettings settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;

			if (settings == null)
			{
				return;
			}
			
			Lookup.Clear();
			Nodes = new IResolvedNode[0];
			
			List<IResolvedNode> nodes = new List<IResolvedNode>();

			for (int i = 0; i < settings.groups.Count; ++i)
			{
				AddressableAssetGroup group = settings.groups[i];
				EditorUtility.DisplayProgressBar("AddressableAssetGroupTempCache", $"Getting dependencies for {group.Name}", i / (float)(settings.groups.Count));
				AddressableAssetGroupDependencyNode node = new AddressableAssetGroupDependencyNode();
				node.groupId = group.Name;

				int g = 0;
				
				foreach (AddressableAssetEntry addressableAssetEntry in group.entries)
				{
					List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>();
					addressableAssetEntry.GatherAllAssets(entries, true, true, false);
					
					foreach (AddressableAssetEntry assetEntry in entries)
					{
						string assetId = NodeDependencyLookupUtility.GetAssetIdForAsset(assetEntry.MainAsset);
						string componentName = "GroupUsage " + g++;
						node.Dependencies.Add(new Dependency(assetId, ConnectionType, AssetNodeType.Name, new []{new PathSegment(componentName, PathSegmentType.Property), }));
					}
				}

				nodes.Add(node);
				Lookup.Add(node.Id, node);
			}

			Nodes = nodes.ToArray();
		}

		public void AddExistingNodes(List<IResolvedNode> nodes)
		{
			foreach (IResolvedNode node in Nodes)
			{
				if (node.Existing)
				{
					nodes.Add(node);
				}
			}
		}

		public string GetHandledNodeType()
		{
			return AddressableGroupNodeType.Name;
		}

		public List<Dependency> GetDependenciesForId(string id)
		{
			if(NodeDependencyLookupUtility.IsResolverActive(_createdDependencyCache, AddressableAssetGroupResolver.Id, ConnectionType))	
			{
				return Lookup[id].Dependencies;
			}
			
			return new List<Dependency>();
		}

		public void Load(string directory)
		{
		}

		public void Save(string directory)
		{
		}

		public void InitLookup()
		{
		}

		public Type GetResolverType()
		{
			return typeof(IAddressableGroupResolver);
		}
	}
	
	public interface IAddressableGroupResolver : IDependencyResolver
	{
	}

	public class AddressableAssetGroupResolver : IAddressableGroupResolver
	{
		public const string Id = "AddressableAssetGroupResolver";
		
		private string[] ConnectionTypes = {AddressableAssetGroupTempCache.ConnectionType};
		private static ConnectionType DependencyType = new ConnectionType(new Color(0.85f, 0.65f, 0.55f), false, true);
		
		public string[] GetConnectionTypes()
		{
			return ConnectionTypes;
		}

		public string GetId()
		{
			return Id;
		}

		public ConnectionType GetDependencyTypeForId(string typeId)
		{
			return DependencyType;
		}
	}
}
