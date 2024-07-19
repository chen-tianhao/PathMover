# O²DESNET.PathMover
A Path Mover implementation library based on O2DESNet framework.

# Change Log

## Version 1.0.4
Model revise: Since overtake is not allowed, only the 1st vehicle in OutPengList can be added to relevant InPendingList. 

## Version 1.0.2
Bug fix: Add PengingPath to interface IVehicle, to manage item inside path.InPendingList in event AttemptToDepart.

## Version 1.0.1
Bug fix: Attempt to depart the specific vehicle in currentPath.InPendingList (rather than the 1st one in previousPath.OutPendingList) when release of capacity propagate backward.

## Version 1.0.0
The 1st published version
