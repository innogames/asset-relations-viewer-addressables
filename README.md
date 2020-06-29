# asset-relations-viewer-addressables

Addon for AssetRelationsViewer to support the unity addressable system.

Tested with addressables version 1.2.2 and up.
Not compatible with verified version 1.1.x!

## Package dependencies

* com.innogames.asset-relations-viewer
* com.unity.addressables

## AddressableAssetGroup

Addressable asset groups and their containing assets can be viewed.
To view dependencies between addressable asset groups and assets the AddressableGroupTempCache/AddressableGroupResolver/AddressableGroupUsage needs to be activated in the cache/resolver list.

![](Docs~/Images/arv_addressables_addressable_group.png)

#### Limitations

Currently only displays groups in the default settings group.

<br><br><br><br>
## AssetReferences

Assets referenced by an AssetReference can be viewed.
To view assets referenced by an AssetReference the AssetDependencyCache/AddressableReferenceResolver/Addressable needs to be activated in the cache/resolver list.

![](Docs~/Images/arv_addressables_assetreference.png)

<br><br><br><br>


