using UnityEngine;

public interface ISync
{

    public int GetId();

    public void SyncRigidBody(SyncData updatedPosition);

    public bool ShouldSync();

    public void DoSync();

    public SyncData FetchRigidBody();

}
