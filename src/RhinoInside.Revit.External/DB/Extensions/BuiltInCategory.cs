using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using GH_IO.Serialization;

namespace RhinoInside.Revit.External.DB.Extensions
{
  public static partial class BuiltInCategoryExtension
  {
#if REVIT_2020
    private static readonly SortedSet<BuiltInCategory> builtInCategories =
      new SortedSet<BuiltInCategory>
      (
        Enum.GetValues(typeof(BuiltInCategory)).
        Cast<BuiltInCategory>().
        Where(Category.IsBuiltInCategoryValid)
      );
#else
    static readonly BuiltInCategory[] validBuiltInCategories =
    {
      BuiltInCategory.OST_PointClouds,
      BuiltInCategory.OST_AssemblyOrigin_Lines,
      BuiltInCategory.OST_AssemblyOrigin_Planes,
      BuiltInCategory.OST_AssemblyOrigin_Points,
      BuiltInCategory.OST_AssemblyOrigin,
      BuiltInCategory.OST_LinksAnalytical,
      BuiltInCategory.OST_FoundationSlabAnalyticalTags,
      BuiltInCategory.OST_WallFoundationAnalyticalTags,
      BuiltInCategory.OST_IsolatedFoundationAnalyticalTags,
      BuiltInCategory.OST_WallAnalyticalTags,
      BuiltInCategory.OST_FloorAnalyticalTags,
      BuiltInCategory.OST_ColumnAnalyticalTags,
      BuiltInCategory.OST_BraceAnalyticalTags,
      BuiltInCategory.OST_BeamAnalyticalTags,
      BuiltInCategory.OST_AnalyticalNodes_Lines,
      BuiltInCategory.OST_AnalyticalNodes_Planes,
      BuiltInCategory.OST_AnalyticalNodes_Points,
      BuiltInCategory.OST_AnalyticalNodes,
      BuiltInCategory.OST_RigidLinksAnalytical,
      BuiltInCategory.OST_FoundationSlabAnalytical,
      BuiltInCategory.OST_WallFoundationAnalytical,
      BuiltInCategory.OST_IsolatedFoundationAnalytical,
      BuiltInCategory.OST_WallAnalytical,
      BuiltInCategory.OST_FloorAnalytical,
      BuiltInCategory.OST_ColumnEndSegment,
      BuiltInCategory.OST_ColumnStartSegment,
      BuiltInCategory.OST_ColumnAnalytical,
      BuiltInCategory.OST_BraceEndSegment,
      BuiltInCategory.OST_BraceStartSegment,
      BuiltInCategory.OST_BraceAnalytical,
      BuiltInCategory.OST_BeamEndSegment,
      BuiltInCategory.OST_BeamStartSegment,
      BuiltInCategory.OST_BeamAnalytical,
      BuiltInCategory.OST_CompassSecondaryMonth,
      BuiltInCategory.OST_CompassPrimaryMonth,
      BuiltInCategory.OST_CompassSectionFilled,
      BuiltInCategory.OST_LightLine,
      BuiltInCategory.OST_MultiSurface,
      BuiltInCategory.OST_SunSurface,
      BuiltInCategory.OST_Analemma,
      BuiltInCategory.OST_SunsetText,
      BuiltInCategory.OST_CompassSection,
      BuiltInCategory.OST_CompassOuter,
      BuiltInCategory.OST_SunriseText,
      BuiltInCategory.OST_CompassInner,
      BuiltInCategory.OST_SunPath2,
      BuiltInCategory.OST_SunPath1,
      BuiltInCategory.OST_Sun,
      BuiltInCategory.OST_SunStudy,
      BuiltInCategory.OST_StructuralTrussStickSymbols,
      BuiltInCategory.OST_TrussChord,
      BuiltInCategory.OST_TrussWeb,
      BuiltInCategory.OST_TrussBottomChordCurve,
      BuiltInCategory.OST_TrussTopChordCurve,
      BuiltInCategory.OST_TrussVertWebCurve,
      BuiltInCategory.OST_TrussDiagWebCurve,
      BuiltInCategory.OST_Truss,
      BuiltInCategory.OST_MassHiddenLines,
      BuiltInCategory.OST_CurtaSystemHiddenLines,
      BuiltInCategory.OST_EntourageHiddenLines,
      BuiltInCategory.OST_PlantingHiddenLines,
      BuiltInCategory.OST_SpecialityEquipmentHiddenLines,
      BuiltInCategory.OST_TopographyHiddenLines,
      BuiltInCategory.OST_SiteHiddenLines,
      BuiltInCategory.OST_RoadsHiddenLines,
      BuiltInCategory.OST_ParkingHiddenLines,
      BuiltInCategory.OST_PlumbingFixturesHiddenLines,
      BuiltInCategory.OST_MechanicalEquipmentHiddenLines,
      BuiltInCategory.OST_LightingFixturesHiddenLines,
      BuiltInCategory.OST_FurnitureSystemsHiddenLines,
      BuiltInCategory.OST_ElectricalFixturesHiddenLines,
      BuiltInCategory.OST_ElectricalEquipmentHiddenLines,
      BuiltInCategory.OST_CaseworkHiddenLines,
      BuiltInCategory.OST_DetailComponentsHiddenLines,
      BuiltInCategory.OST_ShaftOpeningHiddenLines,
      BuiltInCategory.OST_GenericModelHiddenLines,
      BuiltInCategory.OST_CurtainWallMullionsHiddenLines,
      BuiltInCategory.OST_CurtainWallPanelsHiddenLines,
      BuiltInCategory.OST_RampsHiddenLines,
      BuiltInCategory.OST_StairsRailingHiddenLines,
      BuiltInCategory.OST_StairsHiddenLines,
      BuiltInCategory.OST_ColumnsHiddenLines,
      BuiltInCategory.OST_FurnitureHiddenLines,
      BuiltInCategory.OST_LinesHiddenLines,
      BuiltInCategory.OST_CeilingsHiddenLines,
      BuiltInCategory.OST_RoofsHiddenLines,
      BuiltInCategory.OST_DoorsHiddenLines,
      BuiltInCategory.OST_WindowsHiddenLines,
#if REVIT_2019
      BuiltInCategory.OST_StructConnectionProfilesTags,
      BuiltInCategory.OST_StructConnectionHoleTags,
#endif
      BuiltInCategory.OST_CouplerHiddenLines,
      BuiltInCategory.OST_CouplerTags,
      BuiltInCategory.OST_Coupler,
#if REVIT_2019
      BuiltInCategory.OST_StructConnectionWeldTags,
      BuiltInCategory.OST_StructConnectionShearStudTags,
      BuiltInCategory.OST_StructConnectionAnchorTags,
      BuiltInCategory.OST_StructConnectionBoltTags,
      BuiltInCategory.OST_StructConnectionPlateTags,
#endif
      BuiltInCategory.OST_RebarHiddenLines,
#if REVIT_2019
      BuiltInCategory.OST_StructSubConnections,
      BuiltInCategory.OST_StructConnectionModifiers,
      BuiltInCategory.OST_StructConnectionWelds,
      BuiltInCategory.OST_StructConnectionHoles,
      BuiltInCategory.OST_StructConnectionShearStuds,
#endif
#if REVIT_2018
      BuiltInCategory.OST_StructConnectionNobleWarning,
#endif
      BuiltInCategory.OST_StructConnectionOthers,
      BuiltInCategory.OST_StructConnectionBolts,
      BuiltInCategory.OST_StructConnectionTags,
      BuiltInCategory.OST_StructConnectionAnchors,
      BuiltInCategory.OST_StructConnectionPlates,
      BuiltInCategory.OST_StructConnectionProfiles,
      BuiltInCategory.OST_StructConnectionReference,
      BuiltInCategory.OST_StructConnectionFailed,
      BuiltInCategory.OST_StructConnectionStale,
      BuiltInCategory.OST_StructConnectionSymbol,
      BuiltInCategory.OST_StructConnectionHiddenLines,
      BuiltInCategory.OST_StructWeldLines,
      BuiltInCategory.OST_StructConnections,
      BuiltInCategory.OST_FabricAreaBoundary,
      BuiltInCategory.OST_FabricReinSpanSymbol,
      BuiltInCategory.OST_FabricReinforcementWire,
      BuiltInCategory.OST_FabricReinforcementBoundary,
      BuiltInCategory.OST_RebarSetToggle,
      BuiltInCategory.OST_FabricAreaTags,
      BuiltInCategory.OST_FabricReinforcementTags,
      BuiltInCategory.OST_AreaReinTags,
      BuiltInCategory.OST_RebarTags,
      BuiltInCategory.OST_FabricAreaSketchSheetsLines,
      BuiltInCategory.OST_FabricAreaSketchEnvelopeLines,
      BuiltInCategory.OST_FabricAreas,
      BuiltInCategory.OST_FabricReinforcement,
      BuiltInCategory.OST_RebarCover,
      BuiltInCategory.OST_CoverType,
      BuiltInCategory.OST_RebarShape,
      BuiltInCategory.OST_PathReinBoundary,
      BuiltInCategory.OST_PathReinTags,
      BuiltInCategory.OST_PathReinSpanSymbol,
      BuiltInCategory.OST_PathRein,
      BuiltInCategory.OST_Cage,
      BuiltInCategory.OST_AreaReinXVisibility,
      BuiltInCategory.OST_AreaReinBoundary,
      BuiltInCategory.OST_AreaReinSpanSymbol,
      BuiltInCategory.OST_AreaReinSketchOverride,
      BuiltInCategory.OST_AreaRein,
      BuiltInCategory.OST_RebarLines,
      BuiltInCategory.OST_RebarSketchLines,
      BuiltInCategory.OST_Rebar,
      BuiltInCategory.OST_FabricationPipeworkInsulation,
      BuiltInCategory.OST_FabricationDuctworkLining,
      BuiltInCategory.OST_FabricationContainmentDrop,
      BuiltInCategory.OST_FabricationContainmentRise,
      BuiltInCategory.OST_FabricationPipeworkDrop,
      BuiltInCategory.OST_FabricationPipeworkRise,
      BuiltInCategory.OST_FabricationContainmentSymbology,
      BuiltInCategory.OST_FabricationContainmentCenterLine,
      BuiltInCategory.OST_FabricationContainmentTags,
      BuiltInCategory.OST_FabricationContainment,
      BuiltInCategory.OST_FabricationPipeworkSymbology,
      BuiltInCategory.OST_FabricationPipeworkCenterLine,
      BuiltInCategory.OST_FabricationPipeworkTags,
      BuiltInCategory.OST_FabricationPipework,
      BuiltInCategory.OST_FabricationDuctworkSymbology,
      BuiltInCategory.OST_FabricationDuctworkDrop,
      BuiltInCategory.OST_FabricationDuctworkRise,
      BuiltInCategory.OST_FabricationHangerTags,
      BuiltInCategory.OST_FabricationHangers,
      BuiltInCategory.OST_OBSOLETE_FabricationPartsTmpGraphicDropDrag,
      BuiltInCategory.OST_FabricationPartsTmpGraphicDrag,
      BuiltInCategory.OST_OBSOLETE_FabricationPartsTmpGraphicDrop,
      BuiltInCategory.OST_FabricationPartsTmpGraphicEnd,
      BuiltInCategory.OST_FabricationDuctworkInsulation,
      BuiltInCategory.OST_LayoutNodes,
      BuiltInCategory.OST_FabricationDuctworkCenterLine,
      BuiltInCategory.OST_FabricationDuctworkTags,
      BuiltInCategory.OST_FabricationDuctwork,
      BuiltInCategory.OST_LayoutPathBase_Pipings,
      BuiltInCategory.OST_DivisionRules,
      BuiltInCategory.OST_gbXML_Shade,
      BuiltInCategory.OST_AnalyticSurfaces,
      BuiltInCategory.OST_AnalyticSpaces,
      BuiltInCategory.OST_gbXML_OpeningAir,
      BuiltInCategory.OST_gbXML_NonSlidingDoor,
      BuiltInCategory.OST_gbXML_SlidingDoor,
      BuiltInCategory.OST_gbXML_OperableSkylight,
      BuiltInCategory.OST_gbXML_FixedSkylight,
      BuiltInCategory.OST_gbXML_OperableWindow,
      BuiltInCategory.OST_gbXML_FixedWindow,
      BuiltInCategory.OST_gbXML_UndergroundCeiling,
      BuiltInCategory.OST_gbXML_UndergroundSlab,
      BuiltInCategory.OST_gbXML_UndergroundWall,
      BuiltInCategory.OST_gbXML_SurfaceAir,
      BuiltInCategory.OST_gbXML_Ceiling,
      BuiltInCategory.OST_gbXML_InteriorFloor,
      BuiltInCategory.OST_gbXML_InteriorWall,
      BuiltInCategory.OST_gbXML_SlabOnGrade,
      BuiltInCategory.OST_gbXML_RaisedFloor,
      BuiltInCategory.OST_gbXML_Roof,
      BuiltInCategory.OST_gbXML_ExteriorWall,
      BuiltInCategory.OST_DivisionProfile,
      BuiltInCategory.OST_PipeSegments,
      BuiltInCategory.OST_GraphicalWarning_OpenConnector,
      BuiltInCategory.OST_PlaceHolderPipes,
      BuiltInCategory.OST_PlaceHolderDucts,
      BuiltInCategory.OST_PipingSystem_Reference_Visibility,
      BuiltInCategory.OST_PipingSystem_Reference,
      BuiltInCategory.OST_DuctSystem_Reference_Visibility,
      BuiltInCategory.OST_DuctSystem_Reference,
      BuiltInCategory.OST_PipeInsulationsTags,
      BuiltInCategory.OST_DuctLiningsTags,
      BuiltInCategory.OST_DuctInsulationsTags,
      BuiltInCategory.OST_ElectricalInternalCircuits,
      BuiltInCategory.OST_PanelScheduleGraphics,
      BuiltInCategory.OST_CableTrayRun,
      BuiltInCategory.OST_ConduitRun,
      BuiltInCategory.OST_ParamElemElectricalLoadClassification,
      BuiltInCategory.OST_DataPanelScheduleTemplates,
      BuiltInCategory.OST_SwitchboardScheduleTemplates,
      BuiltInCategory.OST_BranchPanelScheduleTemplates,
      BuiltInCategory.OST_ConduitStandards,
      BuiltInCategory.OST_ElectricalLoadClassifications,
      BuiltInCategory.OST_ElectricalDemandFactorDefinitions,
      BuiltInCategory.OST_ConduitFittingCenterLine,
      BuiltInCategory.OST_CableTrayFittingCenterLine,
      BuiltInCategory.OST_ConduitCenterLine,
      BuiltInCategory.OST_ConduitDrop,
      BuiltInCategory.OST_ConduitRiseDrop,
      BuiltInCategory.OST_CableTrayCenterLine,
      BuiltInCategory.OST_CableTrayDrop,
      BuiltInCategory.OST_CableTrayRiseDrop,
      BuiltInCategory.OST_ConduitTags,
      BuiltInCategory.OST_Conduit,
      BuiltInCategory.OST_CableTrayTags,
      BuiltInCategory.OST_CableTray,
      BuiltInCategory.OST_ConduitFittingTags,
      BuiltInCategory.OST_ConduitFitting,
      BuiltInCategory.OST_CableTrayFittingTags,
      BuiltInCategory.OST_CableTrayFitting,
      BuiltInCategory.OST_RoutingPreferences,
      BuiltInCategory.OST_DuctLinings,
      BuiltInCategory.OST_DuctInsulations,
      BuiltInCategory.OST_PipeInsulations,
      BuiltInCategory.OST_HVAC_Load_Schedules,
      BuiltInCategory.OST_HVAC_Load_Building_Types,
      BuiltInCategory.OST_HVAC_Load_Space_Types,
      BuiltInCategory.OST_HVAC_Zones_Reference_Visibility,
      BuiltInCategory.OST_HVAC_Zones_InteriorFill_Visibility,
      BuiltInCategory.OST_HVAC_Zones_ColorFill,
      BuiltInCategory.OST_ZoneTags,
      BuiltInCategory.OST_LayoutPath_Bases,
      BuiltInCategory.OST_WireTemperatureRatings,
      BuiltInCategory.OST_WireInsulations,
      BuiltInCategory.OST_WireMaterials,
      BuiltInCategory.OST_HVAC_Zones_Reference,
      BuiltInCategory.OST_HVAC_Zones_InteriorFill,
      BuiltInCategory.OST_HVAC_Zones_Boundary,
      BuiltInCategory.OST_HVAC_Zones,
      BuiltInCategory.OST_Fluids,
      BuiltInCategory.OST_PipeSchedules,
      BuiltInCategory.OST_PipeMaterials,
      BuiltInCategory.OST_PipeConnections,
      BuiltInCategory.OST_EAConstructions,
      BuiltInCategory.OST_SwitchSystem,
      BuiltInCategory.OST_SprinklerTags,
      BuiltInCategory.OST_Sprinklers,
      BuiltInCategory.OST_RouteCurveBranch,
      BuiltInCategory.OST_RouteCurveMain,
      BuiltInCategory.OST_RouteCurve,
      BuiltInCategory.OST_GbXML_Opening,
      BuiltInCategory.OST_GbXML_SType_Underground,
      BuiltInCategory.OST_GbXML_SType_Shade,
      BuiltInCategory.OST_GbXML_SType_Exterior,
      BuiltInCategory.OST_GbXML_SType_Interior,
      BuiltInCategory.OST_GbXMLFaces,
      BuiltInCategory.OST_WireHomeRunArrows,
      BuiltInCategory.OST_LightingDeviceTags,
      BuiltInCategory.OST_LightingDevices,
      BuiltInCategory.OST_FireAlarmDeviceTags,
      BuiltInCategory.OST_FireAlarmDevices,
      BuiltInCategory.OST_DataDeviceTags,
      BuiltInCategory.OST_DataDevices,
      BuiltInCategory.OST_CommunicationDeviceTags,
      BuiltInCategory.OST_CommunicationDevices,
      BuiltInCategory.OST_SecurityDeviceTags,
      BuiltInCategory.OST_SecurityDevices,
      BuiltInCategory.OST_NurseCallDeviceTags,
      BuiltInCategory.OST_NurseCallDevices,
      BuiltInCategory.OST_TelephoneDeviceTags,
      BuiltInCategory.OST_TelephoneDevices,
      BuiltInCategory.OST_WireTickMarks,
      BuiltInCategory.OST_PipeFittingInsulation,
      BuiltInCategory.OST_PipeFittingCenterLine,
      BuiltInCategory.OST_FlexPipeCurvesInsulation,
      BuiltInCategory.OST_PipeCurvesInsulation,
      BuiltInCategory.OST_PipeCurvesDrop,
      BuiltInCategory.OST_DuctFittingLining,
      BuiltInCategory.OST_DuctFittingInsulation,
      BuiltInCategory.OST_DuctFittingCenterLine,
      BuiltInCategory.OST_FlexDuctCurvesInsulation,
      BuiltInCategory.OST_DuctCurvesLining,
      BuiltInCategory.OST_DuctCurvesInsulation,
      BuiltInCategory.OST_DuctCurvesDrop,
      BuiltInCategory.OST_DuctFittingTags,
      BuiltInCategory.OST_PipeFittingTags,
      BuiltInCategory.OST_PipeColorFills,
      BuiltInCategory.OST_PipeColorFillLegends,
      BuiltInCategory.OST_WireTags,
      BuiltInCategory.OST_PipeAccessoryTags,
      BuiltInCategory.OST_PipeAccessory,
      BuiltInCategory.OST_PipeCurvesRiseDrop,
      BuiltInCategory.OST_FlexPipeCurvesPattern,
      BuiltInCategory.OST_FlexPipeCurvesContour,
      BuiltInCategory.OST_FlexPipeCurvesCenterLine,
      BuiltInCategory.OST_FlexPipeCurves,
      BuiltInCategory.OST_PipeFitting,
      BuiltInCategory.OST_FlexPipeTags,
      BuiltInCategory.OST_PipeTags,
      BuiltInCategory.OST_PipeCurvesContour,
      BuiltInCategory.OST_PipeCurvesCenterLine,
      BuiltInCategory.OST_PipeCurves,
      BuiltInCategory.OST_PipingSystem,
      BuiltInCategory.OST_ElectricalDemandFactor,
      BuiltInCategory.OST_ElecDistributionSys,
      BuiltInCategory.OST_ElectricalVoltage,
      BuiltInCategory.OST_Wire,
      BuiltInCategory.OST_ElectricalCircuit,
      BuiltInCategory.OST_DuctCurvesRiseDrop,
      BuiltInCategory.OST_FlexDuctCurvesPattern,
      BuiltInCategory.OST_FlexDuctCurvesContour,
      BuiltInCategory.OST_FlexDuctCurvesCenterLine,
      BuiltInCategory.OST_FlexDuctCurves,
      BuiltInCategory.OST_DuctAccessoryTags,
      BuiltInCategory.OST_DuctAccessory,
      BuiltInCategory.OST_DuctSystem,
      BuiltInCategory.OST_DuctTerminalTags,
      BuiltInCategory.OST_DuctTerminal,
      BuiltInCategory.OST_DuctFitting,
      BuiltInCategory.OST_DuctColorFills,
      BuiltInCategory.OST_FlexDuctTags,
      BuiltInCategory.OST_DuctTags,
      BuiltInCategory.OST_DuctCurvesContour,
      BuiltInCategory.OST_DuctCurvesCenterLine,
      BuiltInCategory.OST_DuctCurves,
      BuiltInCategory.OST_DuctColorFillLegends,
      BuiltInCategory.OST_ConnectorElemZAxis,
      BuiltInCategory.OST_ConnectorElemYAxis,
      BuiltInCategory.OST_ConnectorElemXAxis,
      BuiltInCategory.OST_ConnectorElem,
#if REVIT_2018
      BuiltInCategory.OST_BridgeBearingTags,
      BuiltInCategory.OST_BridgeGirderTags,
      BuiltInCategory.OST_BridgeFoundationTags,
      BuiltInCategory.OST_BridgeDeckTags,
      BuiltInCategory.OST_BridgeArchTags,
      BuiltInCategory.OST_BridgeCableTags,
      BuiltInCategory.OST_BridgeTowerTags,
      BuiltInCategory.OST_BridgePierTags,
      BuiltInCategory.OST_BridgeAbutmentTags,
      BuiltInCategory.OST_BridgeBearingHiddenLines,
      BuiltInCategory.OST_BridgeGirderHiddenLines,
      BuiltInCategory.OST_BridgeFoundationHiddenLines,
      BuiltInCategory.OST_BridgeDeckHiddenLines,
      BuiltInCategory.OST_BridgeArchHiddenLines,
      BuiltInCategory.OST_BridgeCableHiddenLines,
      BuiltInCategory.OST_BridgeTowerHiddenLines,
      BuiltInCategory.OST_BridgePierHiddenLines,
      BuiltInCategory.OST_BridgeAbutmentHiddenLines,
      BuiltInCategory.OST_BridgeBearings,
      BuiltInCategory.OST_BridgeGirders,
      BuiltInCategory.OST_BridgeFoundations,
      BuiltInCategory.OST_BridgeDecks,
      BuiltInCategory.OST_BridgeArches,
      BuiltInCategory.OST_BridgeCables,
      BuiltInCategory.OST_BridgeTowers,
      BuiltInCategory.OST_BridgePiers,
      BuiltInCategory.OST_BridgeAbutments,
#endif
      BuiltInCategory.OST_DesignOptions,
      BuiltInCategory.OST_DesignOptionSets,
      BuiltInCategory.OST_StructuralBracePlanReps,
      BuiltInCategory.OST_StructConnectionSymbols,
      BuiltInCategory.OST_StructuralAnnotations,
      BuiltInCategory.OST_RevisionCloudTags,
      BuiltInCategory.OST_Revisions,
      BuiltInCategory.OST_RevisionClouds,
      BuiltInCategory.OST_EditCutProfile,
      BuiltInCategory.OST_ElevationMarks,
      BuiltInCategory.OST_GridHeads,
      BuiltInCategory.OST_LevelHeads,
      BuiltInCategory.OST_VolumeOfInterest,
      BuiltInCategory.OST_BoundaryConditions,
      BuiltInCategory.OST_InternalAreaLoadTags,
      BuiltInCategory.OST_InternalLineLoadTags,
      BuiltInCategory.OST_InternalPointLoadTags,
      BuiltInCategory.OST_AreaLoadTags,
      BuiltInCategory.OST_LineLoadTags,
      BuiltInCategory.OST_PointLoadTags,
      BuiltInCategory.OST_LoadCasesSeismic,
      BuiltInCategory.OST_LoadCasesTemperature,
      BuiltInCategory.OST_LoadCasesAccidental,
      BuiltInCategory.OST_LoadCasesRoofLive,
      BuiltInCategory.OST_LoadCasesSnow,
      BuiltInCategory.OST_LoadCasesWind,
      BuiltInCategory.OST_LoadCasesLive,
      BuiltInCategory.OST_LoadCasesDead,
      BuiltInCategory.OST_LoadCases,
      BuiltInCategory.OST_InternalAreaLoads,
      BuiltInCategory.OST_InternalLineLoads,
      BuiltInCategory.OST_InternalPointLoads,
      BuiltInCategory.OST_InternalLoads,
      BuiltInCategory.OST_AreaLoads,
      BuiltInCategory.OST_LineLoads,
      BuiltInCategory.OST_PointLoads,
      BuiltInCategory.OST_Loads,
      BuiltInCategory.OST_BeamSystemTags,
      BuiltInCategory.OST_FootingSpanDirectionSymbol,
      BuiltInCategory.OST_SpanDirectionSymbol,
      BuiltInCategory.OST_SpotElevSymbols,
      BuiltInCategory.OST_TrussTags,
      BuiltInCategory.OST_KeynoteTags,
      BuiltInCategory.OST_DetailComponentTags,
      BuiltInCategory.OST_MaterialTags,
      BuiltInCategory.OST_FloorTags,
      BuiltInCategory.OST_CurtaSystemTags,
      BuiltInCategory.OST_StairsTags,
      BuiltInCategory.OST_MultiCategoryTags,
      BuiltInCategory.OST_PlantingTags,
      BuiltInCategory.OST_AreaTags,
      BuiltInCategory.OST_StructuralFoundationTags,
      BuiltInCategory.OST_StructuralColumnTags,
      BuiltInCategory.OST_ParkingTags,
      BuiltInCategory.OST_SiteTags,
      BuiltInCategory.OST_StructuralFramingTags,
      BuiltInCategory.OST_SpecialityEquipmentTags,
      BuiltInCategory.OST_GenericModelTags,
      BuiltInCategory.OST_CurtainWallPanelTags,
      BuiltInCategory.OST_WallTags,
      BuiltInCategory.OST_PlumbingFixtureTags,
      BuiltInCategory.OST_MechanicalEquipmentTags,
      BuiltInCategory.OST_LightingFixtureTags,
      BuiltInCategory.OST_FurnitureSystemTags,
      BuiltInCategory.OST_FurnitureTags,
      BuiltInCategory.OST_ElectricalFixtureTags,
      BuiltInCategory.OST_ElectricalEquipmentTags,
      BuiltInCategory.OST_CeilingTags,
      BuiltInCategory.OST_CaseworkTags,
      BuiltInCategory.OST_MEPSpaceColorFill,
      BuiltInCategory.OST_MEPSpaceReference,
      BuiltInCategory.OST_MEPSpaceInteriorFill,
      BuiltInCategory.OST_MEPSpaceReferenceVisibility,
      BuiltInCategory.OST_MEPSpaceInteriorFillVisibility,
      BuiltInCategory.OST_MEPSpaces,
      BuiltInCategory.OST_StackedWalls,
      BuiltInCategory.OST_MassGlazingAll,
      BuiltInCategory.OST_MassFloorsAll,
      BuiltInCategory.OST_MassWallsAll,
      BuiltInCategory.OST_MassExteriorWallUnderground,
      BuiltInCategory.OST_MassSlab,
      BuiltInCategory.OST_MassShade,
      BuiltInCategory.OST_MassOpening,
      BuiltInCategory.OST_MassSkylights,
      BuiltInCategory.OST_MassGlazing,
      BuiltInCategory.OST_MassRoof,
      BuiltInCategory.OST_MassExteriorWall,
      BuiltInCategory.OST_MassInteriorWall,
      BuiltInCategory.OST_MassZone,
      BuiltInCategory.OST_MassAreaFaceTags,
      BuiltInCategory.OST_HostTemplate,
      BuiltInCategory.OST_MassFaceSplitter,
      BuiltInCategory.OST_MassCutter,
      BuiltInCategory.OST_ZoningEnvelope,
      BuiltInCategory.OST_MassTags,
      BuiltInCategory.OST_MassForm,
      BuiltInCategory.OST_MassFloor,
      BuiltInCategory.OST_Mass,
      BuiltInCategory.OST_DividedSurface_DiscardedDivisionLines,
      BuiltInCategory.OST_DividedSurfaceBelt,
      BuiltInCategory.OST_TilePatterns,
      BuiltInCategory.OST_AlwaysExcludedInAllViews,
      BuiltInCategory.OST_DividedSurface_TransparentFace,
      BuiltInCategory.OST_DividedSurface_PreDividedSurface,
      BuiltInCategory.OST_DividedSurface_PatternFill,
      BuiltInCategory.OST_DividedSurface_PatternLines,
      BuiltInCategory.OST_DividedSurface_Gridlines,
      BuiltInCategory.OST_DividedSurface_Nodes,
      BuiltInCategory.OST_DividedSurface,
      BuiltInCategory.OST_RampsDownArrow,
      BuiltInCategory.OST_RampsUpArrow,
      BuiltInCategory.OST_RampsDownText,
      BuiltInCategory.OST_RampsUpText,
      BuiltInCategory.OST_RampsStringerAboveCut,
      BuiltInCategory.OST_RampsStringer,
      BuiltInCategory.OST_RampsAboveCut,
      BuiltInCategory.OST_ZoneSchemes,
      BuiltInCategory.OST_AreaSchemes,
      BuiltInCategory.OST_Areas,
      BuiltInCategory.OST_ProjectInformation,
      BuiltInCategory.OST_Sheets,
      BuiltInCategory.OST_ProfileFamilies,
      BuiltInCategory.OST_DetailComponents,
      BuiltInCategory.OST_RoofSoffit,
      BuiltInCategory.OST_EdgeSlab,
      BuiltInCategory.OST_Gutter,
      BuiltInCategory.OST_Fascia,
      BuiltInCategory.OST_Entourage,
      BuiltInCategory.OST_Planting,
      BuiltInCategory.OST_Blocks,
      BuiltInCategory.OST_StructuralStiffenerHiddenLines,
      BuiltInCategory.OST_StructuralColumnLocationLine,
      BuiltInCategory.OST_StructuralFramingLocationLine,
      BuiltInCategory.OST_StructuralStiffenerTags,
      BuiltInCategory.OST_StructuralStiffener,
      BuiltInCategory.OST_FootingAnalyticalGeometry,
      BuiltInCategory.OST_RvtLinks,
      BuiltInCategory.OST_SpecialityEquipment,
      BuiltInCategory.OST_ColumnAnalyticalRigidLinks,
      BuiltInCategory.OST_SecondaryTopographyContours,
      BuiltInCategory.OST_TopographyContours,
      BuiltInCategory.OST_TopographySurface,
      BuiltInCategory.OST_Topography,
#if REVIT_2019
      BuiltInCategory.OST_TopographyLink,
#endif
      BuiltInCategory.OST_StructuralTruss,
      BuiltInCategory.OST_StructuralColumnStickSymbols,
      BuiltInCategory.OST_HiddenStructuralColumnLines,
      BuiltInCategory.OST_AnalyticalRigidLinks,
      BuiltInCategory.OST_ColumnAnalyticalGeometry,
      BuiltInCategory.OST_FramingAnalyticalGeometry,
      BuiltInCategory.OST_StructuralColumns,
      BuiltInCategory.OST_HiddenStructuralFramingLines,
      BuiltInCategory.OST_KickerBracing,
      BuiltInCategory.OST_StructuralFramingSystem,
      BuiltInCategory.OST_VerticalBracing,
      BuiltInCategory.OST_HorizontalBracing,
      BuiltInCategory.OST_Purlin,
      BuiltInCategory.OST_Joist,
      BuiltInCategory.OST_Girder,
      BuiltInCategory.OST_StructuralFramingOther,
      BuiltInCategory.OST_StructuralFraming,
      BuiltInCategory.OST_HiddenStructuralFoundationLines,
      BuiltInCategory.OST_StructuralFoundation,
      BuiltInCategory.OST_BasePointAxisZ,
      BuiltInCategory.OST_BasePointAxisY,
      BuiltInCategory.OST_BasePointAxisX,
      BuiltInCategory.OST_SharedBasePoint,
      BuiltInCategory.OST_ProjectBasePoint,
      BuiltInCategory.OST_SitePropertyLineSegmentTags,
      BuiltInCategory.OST_SitePropertyLineSegment,
      BuiltInCategory.OST_SitePropertyTags,
      BuiltInCategory.OST_SitePointBoundary,
      BuiltInCategory.OST_SiteProperty,
      BuiltInCategory.OST_BuildingPad,
      BuiltInCategory.OST_SitePoint,
      BuiltInCategory.OST_Site,
      BuiltInCategory.OST_Roads,
      BuiltInCategory.OST_Parking,
      BuiltInCategory.OST_PlumbingFixtures,
      BuiltInCategory.OST_MechanicalEquipment,
      BuiltInCategory.OST_LightingFixtureSource,
      BuiltInCategory.OST_LightingFixtures,
      BuiltInCategory.OST_FurnitureSystems,
      BuiltInCategory.OST_ElectricalFixtures,
      BuiltInCategory.OST_ElectricalEquipment,
#if REVIT_2020
      BuiltInCategory.OST_ZoneEquipment,
      BuiltInCategory.OST_MEPAnalyticalWaterLoop,
      BuiltInCategory.OST_MEPAnalyticalAirLoop,
      BuiltInCategory.OST_MEPSystemZoneTags,
      BuiltInCategory.OST_MEPSystemZone,
#endif
      BuiltInCategory.OST_Casework,
      BuiltInCategory.OST_ArcWallRectOpening,
      BuiltInCategory.OST_DormerOpeningIncomplete,
      BuiltInCategory.OST_SWallRectOpening,
      BuiltInCategory.OST_ShaftOpening,
      BuiltInCategory.OST_StructuralFramingOpening,
      BuiltInCategory.OST_ColumnOpening,
#if REVIT_2019
      BuiltInCategory.OST_RiseDropSymbols,
      BuiltInCategory.OST_PipeHydronicSeparationSymbols,
      BuiltInCategory.OST_MechanicalEquipmentSetBoundaryLines,
      BuiltInCategory.OST_MechanicalEquipmentSetTags,
      BuiltInCategory.OST_MechanicalEquipmentSet,
#endif
#if REVIT_2018
      BuiltInCategory.OST_AnalyticalPipeConnectionLineSymbol,
      BuiltInCategory.OST_AnalyticalPipeConnections,
      BuiltInCategory.OST_Coordination_Model,
#endif
      BuiltInCategory.OST_MultistoryStairs,
      BuiltInCategory.OST_CoordinateSystem,
      BuiltInCategory.OST_FndSlabLocalCoordSys,
      BuiltInCategory.OST_FloorLocalCoordSys,
      BuiltInCategory.OST_WallLocalCoordSys,
      BuiltInCategory.OST_BraceLocalCoordSys,
      BuiltInCategory.OST_ColumnLocalCoordSys,
      BuiltInCategory.OST_BeamLocalCoordSys,
      BuiltInCategory.OST_MultiReferenceAnnotations,
      BuiltInCategory.OST_NodeAnalyticalTags,
      BuiltInCategory.OST_LinkAnalyticalTags,
      BuiltInCategory.OST_RailingRailPathExtensionLines,
      BuiltInCategory.OST_RailingRailPathLines,
      BuiltInCategory.OST_StairsSupports,
      BuiltInCategory.OST_RailingHandRailAboveCut,
      BuiltInCategory.OST_RailingTopRailAboveCut,
      BuiltInCategory.OST_RailingTermination,
      BuiltInCategory.OST_RailingSupport,
      BuiltInCategory.OST_RailingHandRail,
      BuiltInCategory.OST_RailingTopRail,
      BuiltInCategory.OST_StairsSketchPathLines,
      BuiltInCategory.OST_StairsTriserNumbers,
      BuiltInCategory.OST_StairsSupportTags,
      BuiltInCategory.OST_StairsLandingTags,
      BuiltInCategory.OST_StairsRunTags,
      BuiltInCategory.OST_StairsPathsAboveCut,
      BuiltInCategory.OST_StairsPaths,
      BuiltInCategory.OST_StairsRiserLinesAboveCut,
      BuiltInCategory.OST_StairsRiserLines,
      BuiltInCategory.OST_StairsOutlinesAboveCut,
      BuiltInCategory.OST_StairsOutlines,
      BuiltInCategory.OST_StairsNosingLinesAboveCut,
      BuiltInCategory.OST_StairsNosingLines,
      BuiltInCategory.OST_StairsCutMarksAboveCut,
      BuiltInCategory.OST_StairsCutMarks,
      BuiltInCategory.OST_ComponentRepeaterSlot,
      BuiltInCategory.OST_ComponentRepeater,
      BuiltInCategory.OST_DividedPath,
      BuiltInCategory.OST_IOSRoomCalculationPoint,
      BuiltInCategory.OST_PropertySet,
      BuiltInCategory.OST_StairsTrisers,
      BuiltInCategory.OST_StairsLandings,
      BuiltInCategory.OST_StairsRuns,
      BuiltInCategory.OST_RailingSystemHardware,
      BuiltInCategory.OST_RailingSystemPanel,
      BuiltInCategory.OST_RailingSystemPost,
      BuiltInCategory.OST_RailingSystemSegment,
      BuiltInCategory.OST_AdaptivePoints_Lines,
      BuiltInCategory.OST_AdaptivePoints_Planes,
      BuiltInCategory.OST_AdaptivePoints_Points,
      BuiltInCategory.OST_AdaptivePoints,
      BuiltInCategory.OST_CeilingOpening,
      BuiltInCategory.OST_FloorOpening,
      BuiltInCategory.OST_RoofOpening,
      BuiltInCategory.OST_WallRefPlanes,
      BuiltInCategory.OST_StructLocationLineControl,
#if REVIT_2020
      BuiltInCategory.OST_PathOfTravelTags,
      BuiltInCategory.OST_PathOfTravelLines,
#endif
      BuiltInCategory.OST_DimLockControlLeader,
      BuiltInCategory.OST_MEPSpaceSeparationLines,
      BuiltInCategory.OST_AreaPolylines,
      BuiltInCategory.OST_RoomPolylines,
      BuiltInCategory.OST_InstanceDrivenLineStyle,
      BuiltInCategory.OST_RemovedGridSeg,
      BuiltInCategory.OST_IOSOpening,
      BuiltInCategory.OST_IOSTilePatternGrid,
      BuiltInCategory.OST_ControlLocal,
      BuiltInCategory.OST_ControlAxisZ,
      BuiltInCategory.OST_ControlAxisY,
      BuiltInCategory.OST_ControlAxisX,
      BuiltInCategory.OST_XRayConstrainedProfileEdge,
      BuiltInCategory.OST_XRayImplicitPathCurve,
      BuiltInCategory.OST_XRayPathPoint,
      BuiltInCategory.OST_XRayPathCurve,
      BuiltInCategory.OST_XRaySideEdge,
      BuiltInCategory.OST_XRayProfileEdge,
      BuiltInCategory.OST_ReferencePoints_Lines,
      BuiltInCategory.OST_ReferencePoints_Planes,
      BuiltInCategory.OST_ReferencePoints_Points,
      BuiltInCategory.OST_ReferencePoints,
      BuiltInCategory.OST_Materials,
      BuiltInCategory.OST_CeilingsCutPattern,
      BuiltInCategory.OST_CeilingsDefault,
      BuiltInCategory.OST_CeilingsFinish2,
      BuiltInCategory.OST_CeilingsFinish1,
      BuiltInCategory.OST_CeilingsSubstrate,
      BuiltInCategory.OST_CeilingsInsulation,
      BuiltInCategory.OST_CeilingsStructure,
      BuiltInCategory.OST_CeilingsMembrane,
      BuiltInCategory.OST_FloorsInteriorEdges,
      BuiltInCategory.OST_FloorsCutPattern,
      BuiltInCategory.OST_HiddenFloorLines,
      BuiltInCategory.OST_FloorsDefault,
      BuiltInCategory.OST_FloorsFinish2,
      BuiltInCategory.OST_FloorsFinish1,
      BuiltInCategory.OST_FloorsSubstrate,
      BuiltInCategory.OST_FloorsInsulation,
      BuiltInCategory.OST_FloorsStructure,
      BuiltInCategory.OST_FloorsMembrane,
      BuiltInCategory.OST_RoofsInteriorEdges,
      BuiltInCategory.OST_RoofsCutPattern,
      BuiltInCategory.OST_RoofsDefault,
      BuiltInCategory.OST_RoofsFinish2,
      BuiltInCategory.OST_RoofsFinish1,
      BuiltInCategory.OST_RoofsSubstrate,
      BuiltInCategory.OST_RoofsInsulation,
      BuiltInCategory.OST_RoofsStructure,
      BuiltInCategory.OST_RoofsMembrane,
      BuiltInCategory.OST_WallsCutPattern,
      BuiltInCategory.OST_HiddenWallLines,
      BuiltInCategory.OST_WallsDefault,
      BuiltInCategory.OST_WallsFinish2,
      BuiltInCategory.OST_WallsFinish1,
      BuiltInCategory.OST_WallsSubstrate,
      BuiltInCategory.OST_WallsInsulation,
      BuiltInCategory.OST_WallsStructure,
      BuiltInCategory.OST_WallsMembrane,
      BuiltInCategory.OST_PreviewLegendComponents,
      BuiltInCategory.OST_LegendComponents,
#if REVIT_2018
      BuiltInCategory.OST_Schedules,
#endif
      BuiltInCategory.OST_ScheduleGraphics,
      BuiltInCategory.OST_RasterImages,
      BuiltInCategory.OST_ColorFillSchema,
      BuiltInCategory.OST_RoomColorFill,
      BuiltInCategory.OST_ColorFillLegends,
      BuiltInCategory.OST_AnnotationCropSpecial,
      BuiltInCategory.OST_CropBoundarySpecial,
      BuiltInCategory.OST_AnnotationCrop,
      BuiltInCategory.OST_FloorsAnalyticalGeometry,
      BuiltInCategory.OST_WallsAnalyticalGeometry,
      BuiltInCategory.OST_CalloutLeaderLine,
      BuiltInCategory.OST_CeilingsSurfacePattern,
      BuiltInCategory.OST_RoofsSurfacePattern,
      BuiltInCategory.OST_FloorsSurfacePattern,
      BuiltInCategory.OST_WallsSurfacePattern,
      BuiltInCategory.OST_CalloutBoundary,
      BuiltInCategory.OST_CalloutHeads,
      BuiltInCategory.OST_Callouts,
      BuiltInCategory.OST_CropBoundary,
      BuiltInCategory.OST_Elev,
      BuiltInCategory.OST_AxisZ,
      BuiltInCategory.OST_AxisY,
      BuiltInCategory.OST_AxisX,
      BuiltInCategory.OST_CLines,
      BuiltInCategory.OST_Lights,
      BuiltInCategory.OST_ViewportLabel,
      BuiltInCategory.OST_Viewports,
      BuiltInCategory.OST_Camera_Lines,
      BuiltInCategory.OST_Cameras,
      BuiltInCategory.OST_MEPSpaceTags,
      BuiltInCategory.OST_RoomTags,
      BuiltInCategory.OST_DoorTags,
      BuiltInCategory.OST_WindowTags,
      BuiltInCategory.OST_SectionHeadWideLines,
      BuiltInCategory.OST_SectionHeadMediumLines,
      BuiltInCategory.OST_SectionHeadThinLines,
      BuiltInCategory.OST_SectionHeads,
      BuiltInCategory.OST_ContourLabels,
      BuiltInCategory.OST_CurtaSystemFaceManager,
      BuiltInCategory.OST_CurtaSystem,
      BuiltInCategory.OST_AreaReport_Arc_Minus,
      BuiltInCategory.OST_AreaReport_Arc_Plus,
      BuiltInCategory.OST_AreaReport_Boundary,
      BuiltInCategory.OST_AreaReport_Triangle,
      BuiltInCategory.OST_CurtainGridsCurtaSystem,
      BuiltInCategory.OST_CurtainGridsSystem,
      BuiltInCategory.OST_CurtainGridsWall,
      BuiltInCategory.OST_CurtainGridsRoof,
      BuiltInCategory.OST_AnalysisDisplayStyle,
      BuiltInCategory.OST_AnalysisResults,
      BuiltInCategory.OST_RenderRegions,
      BuiltInCategory.OST_SectionBox,
      BuiltInCategory.OST_TextNotes,
      BuiltInCategory.OST_Divisions,
      BuiltInCategory.OST_CenterLines,
      BuiltInCategory.OST_LinesBeyond,
      BuiltInCategory.OST_HiddenLines,
      BuiltInCategory.OST_DemolishedLines,
      BuiltInCategory.OST_OverheadLines,
      BuiltInCategory.OST_TitleBlockWideLines,
      BuiltInCategory.OST_TitleBlockMediumLines,
      BuiltInCategory.OST_TitleBlockThinLines,
      BuiltInCategory.OST_TitleBlocks,
      BuiltInCategory.OST_Views,
      BuiltInCategory.OST_Viewers,
      BuiltInCategory.OST_PartHiddenLines,
      BuiltInCategory.OST_PartTags,
      BuiltInCategory.OST_Parts,
      BuiltInCategory.OST_AssemblyTags,
      BuiltInCategory.OST_Assemblies,
      BuiltInCategory.OST_RoofTags,
      BuiltInCategory.OST_SpotSlopes,
      BuiltInCategory.OST_SpotCoordinates,
      BuiltInCategory.OST_SpotElevations,
      BuiltInCategory.OST_Constraints,
      BuiltInCategory.OST_WeakDims,
      BuiltInCategory.OST_Dimensions,
      BuiltInCategory.OST_Levels,
      BuiltInCategory.OST_DisplacementPath,
      BuiltInCategory.OST_DisplacementElements,
      BuiltInCategory.OST_GridChains,
      BuiltInCategory.OST_Grids,
      BuiltInCategory.OST_BrokenSectionLine,
      BuiltInCategory.OST_SectionLine,
      BuiltInCategory.OST_Sections,
      BuiltInCategory.OST_ReferenceViewer,
      BuiltInCategory.OST_ReferenceViewerSymbol,
      BuiltInCategory.OST_ImportObjectStyles,
      BuiltInCategory.OST_MaskingRegion,
      BuiltInCategory.OST_Matchline,
      BuiltInCategory.OST_FaceSplitter,
      BuiltInCategory.OST_PlanRegion,
      BuiltInCategory.OST_FilledRegion,
      BuiltInCategory.OST_Massing,
      BuiltInCategory.OST_Reveals,
      BuiltInCategory.OST_Cornices,
      BuiltInCategory.OST_Ramps,
      BuiltInCategory.OST_CurtainGrids,
      BuiltInCategory.OST_CurtainWallMullions,
      BuiltInCategory.OST_CurtainWallPanels,
      BuiltInCategory.OST_AreaReference,
      BuiltInCategory.OST_AreaInteriorFill,
      BuiltInCategory.OST_RoomReference,
      BuiltInCategory.OST_RoomInteriorFill,
      BuiltInCategory.OST_AreaColorFill,
      BuiltInCategory.OST_AreaReferenceVisibility,
      BuiltInCategory.OST_AreaInteriorFillVisibility,
      BuiltInCategory.OST_RoomReferenceVisibility,
      BuiltInCategory.OST_RoomInteriorFillVisibility,
      BuiltInCategory.OST_Rooms,
      BuiltInCategory.OST_GenericModel,
      BuiltInCategory.OST_GenericAnnotation,
      BuiltInCategory.OST_StairsRailingTags,
      BuiltInCategory.OST_StairsRailingAboveCut,
      BuiltInCategory.OST_StairsDownArrows,
      BuiltInCategory.OST_StairsUpArrows,
      BuiltInCategory.OST_StairsDownText,
      BuiltInCategory.OST_StairsRailingRail,
      BuiltInCategory.OST_StairsRailingBaluster,
      BuiltInCategory.OST_StairsRailing,
      BuiltInCategory.OST_StairsUpText,
      BuiltInCategory.OST_StairsSupportsAboveCut,
      BuiltInCategory.OST_StairsStringerCarriage,
      BuiltInCategory.OST_Stairs,
      BuiltInCategory.OST_IOSNavWheelPivotBall,
      BuiltInCategory.OST_IOSRoomComputationHeight,
      BuiltInCategory.OST_IOSRoomUpperLowerLines,
      BuiltInCategory.OST_IOSDragBoxInverted,
      BuiltInCategory.OST_IOSDragBox,
      BuiltInCategory.OST_Phases,
      BuiltInCategory.OST_IOS_GeoSite,
      BuiltInCategory.OST_IOS_GeoLocations,
      BuiltInCategory.OST_IOSFabricReinSpanSymbolCtrl,
      BuiltInCategory.OST_GuideGrid,
      BuiltInCategory.OST_EPS_Future,
      BuiltInCategory.OST_EPS_Temporary,
      BuiltInCategory.OST_EPS_New,
      BuiltInCategory.OST_EPS_Demolished,
      BuiltInCategory.OST_EPS_Existing,
      BuiltInCategory.OST_IOSMeasureLineScreenSize,
      BuiltInCategory.OST_Columns,
      BuiltInCategory.OST_IOSRebarSystemSpanSymbolCtrl,
      BuiltInCategory.OST_IOSRoomTagToRoomLines,
      BuiltInCategory.OST_IOSAttachedDetailGroups,
      BuiltInCategory.OST_IOSDetailGroups,
      BuiltInCategory.OST_IOSModelGroups,
      BuiltInCategory.OST_IOSSuspendedSketch,
      BuiltInCategory.OST_IOSWallCoreBoundary,
      BuiltInCategory.OST_IOSMeasureLine,
      BuiltInCategory.OST_IOSArrays,
      BuiltInCategory.OST_Curtain_Systems,
      BuiltInCategory.OST_IOSBBoxScreenSize,
      BuiltInCategory.OST_IOSSlabShapeEditorPointInterior,
      BuiltInCategory.OST_IOSSlabShapeEditorPointBoundary,
      BuiltInCategory.OST_IOSSlabShapeEditorBoundary,
      BuiltInCategory.OST_IOSSlabShapeEditorAutoCrease,
      BuiltInCategory.OST_IOSSlabShapeEditorExplitCrease,
      BuiltInCategory.OST_ReferenceLines,
      BuiltInCategory.OST_IOSNotSilhouette,
      BuiltInCategory.OST_FillPatterns,
      BuiltInCategory.OST_Furniture,
      BuiltInCategory.OST_AreaSchemeLines,
      BuiltInCategory.OST_GenericLines,
      BuiltInCategory.OST_InsulationLines,
      BuiltInCategory.OST_IOSRoomPerimeterLines,
      BuiltInCategory.OST_IOSCuttingGeometry,
      BuiltInCategory.OST_IOSCrashGraphics,
      BuiltInCategory.OST_IOSGroups,
      BuiltInCategory.OST_IOSGhost,
      BuiltInCategory.OST_StairsSketchLandingCenterLines,
      BuiltInCategory.OST_StairsSketchRunLines,
      BuiltInCategory.OST_StairsSketchRiserLines,
      BuiltInCategory.OST_StairsSketchBoundaryLines,
      BuiltInCategory.OST_RoomSeparationLines,
      BuiltInCategory.OST_AxisOfRotation,
      BuiltInCategory.OST_InvisibleLines,
      BuiltInCategory.OST_IOSThinPixel_DashDot,
      BuiltInCategory.OST_IOSThinPixel_Dash,
      BuiltInCategory.OST_IOSThinPixel_Dot,
      BuiltInCategory.OST_IOS,
      BuiltInCategory.OST_IOSThinPixel,
      BuiltInCategory.OST_IOSFlipControl,
      BuiltInCategory.OST_IOSSketchGrid,
      BuiltInCategory.OST_IOSSuspendedSketch_obsolete,
      BuiltInCategory.OST_IOSDatumPlane,
      BuiltInCategory.OST_Lines,
      BuiltInCategory.OST_IOSAligningLine,
      BuiltInCategory.OST_IOSBackedUpElements,
      BuiltInCategory.OST_IOSRegeneratedElements,
      BuiltInCategory.OST_SketchLines,
      BuiltInCategory.OST_CurvesWideLines,
      BuiltInCategory.OST_CurvesMediumLines,
      BuiltInCategory.OST_CurvesThinLines,
      BuiltInCategory.OST_Ceilings,
      BuiltInCategory.OST_Roofs,
      BuiltInCategory.OST_Floors,
      BuiltInCategory.OST_DoorsGlassProjection,
      BuiltInCategory.OST_DoorsFrameMullionProjection,
      BuiltInCategory.OST_DoorsOpeningProjection,
      BuiltInCategory.OST_DoorsPanelProjection,
      BuiltInCategory.OST_Doors,
      BuiltInCategory.OST_WindowsOpeningProjection,
      BuiltInCategory.OST_WindowsSillHeadProjection,
      BuiltInCategory.OST_WindowsFrameMullionProjection,
      BuiltInCategory.OST_WindowsGlassProjection,
      BuiltInCategory.OST_Windows,
      BuiltInCategory.OST_Walls,
      BuiltInCategory.OST_IOSRegenerationFailure,
    };

    public static readonly SortedSet<BuiltInCategory> builtInCategories =
      new SortedSet<BuiltInCategory>(validBuiltInCategories);
#endif

    /// <summary>
    /// Set of valid <see cref="Autodesk.Revit.DB.BuiltInCategory"/> enum values.
    /// </summary>
    public static IReadOnlyCollection<BuiltInCategory> BuiltInCategories => builtInCategories;

    static Document _HiddenInUIBuiltInCategoriesDocument;
    static BuiltInCategory[] _HiddenInUIBuiltInCategories;

    /// <summary>
    /// Set of hidden <see cref="Autodesk.Revit.DB.BuiltInCategory"/> enum values.
    /// </summary>
    /// <param name="document"></param>
    public static IReadOnlyCollection<BuiltInCategory> GetHiddenInUIBuiltInCategories(Document document)
    {
      if (!document.IsEquivalent(_HiddenInUIBuiltInCategoriesDocument))
      {
        _HiddenInUIBuiltInCategories = BuiltInCategories.Where(x => document.GetCategory(x)?.IsVisibleInUI() != true).ToArray();
        _HiddenInUIBuiltInCategoriesDocument = document;
      }

      return _HiddenInUIBuiltInCategories;
    }

    /// <summary>
    /// Checks if a <see cref="Autodesk.Revit.DB.BuiltInCategory"/> is valid.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsValid(this BuiltInCategory value)
    {
      if (-3000000 < (int) value && (int) value < -2000000)
        return builtInCategories.Contains(value);

      return false;
    }

    public static string Name(this BuiltInCategory value)
    {
      if (value == BuiltInCategory.INVALID) return string.Empty;

#if REVIT_2020
      return LabelUtils.GetLabelFor(value);
#else
      return Definitions.TryGetValue(value, out var definition) ? definition.Name : string.Empty;
#endif
    }

    public static string FullName(this BuiltInCategory value)
    {
      if (!Definitions.TryGetValue(value, out var definition)) return null;
      if (!definition.Parent.IsValid()) return definition.Id.Name();
      return $"{definition.Parent.FullName()}\\{definition.Id.Name()}";
    }

    public static CategoryType CategoryType(this BuiltInCategory value) => Definitions.TryGetValue(value, out var definition) ? definition.CategoryType : Autodesk.Revit.DB.CategoryType.Invalid;
    public static bool IsTagCategory(this BuiltInCategory value) => Definitions.TryGetValue(value, out var definition) ? definition.IsTagCategory : false;

    public static BuiltInCategory Parent(this BuiltInCategory value) => Definitions.TryGetValue(value, out var definition) ? definition.Parent : Autodesk.Revit.DB.BuiltInCategory.INVALID;
    public static bool CanAddSubcategory(this BuiltInCategory value) => Definitions.TryGetValue(value, out var definition) ? definition.CanAddSubcategory : false;

    public static bool AllowsBoundParameters(this BuiltInCategory value) => Definitions.TryGetValue(value, out var definition) ? definition.AllowsBoundParameters : false;
    public static bool HasMaterialQuantities(this BuiltInCategory value) => Definitions.TryGetValue(value, out var definition) ? definition.HasMaterialQuantities : false;
    public static bool IsCuttable(this BuiltInCategory value) => Definitions.TryGetValue(value, out var definition) ? definition.IsCuttable : false;
    public static bool IsVisibleInUI(this BuiltInCategory value) => Definitions.TryGetValue(value, out var definition) ? definition.IsVisibleInUI : false;

    internal readonly partial struct Definition
    {
      public Definition
      (
        string fullName,
        CategoryType categoryType,
        bool isTagCategory,
        BuiltInCategory parent,
        BuiltInCategory id,
        bool canAddSubcategory,
        bool allowsBoundParameters,
        bool hasMaterialQuantities,
        bool isCuttable,
        bool isVisibleInUI
      )
      {
        Name = fullName;

        CategoryType = categoryType;
        IsTagCategory = isTagCategory;

        Parent = parent;
        Id = id;
        CanAddSubcategory = canAddSubcategory;

        AllowsBoundParameters = allowsBoundParameters;
        HasMaterialQuantities = hasMaterialQuantities;
        IsCuttable = isCuttable;
        IsVisibleInUI = isVisibleInUI;
      }

      public readonly string Name;

      public readonly CategoryType CategoryType;
      public readonly bool IsTagCategory;

      public readonly BuiltInCategory Parent;
      public readonly BuiltInCategory Id;
      public readonly bool CanAddSubcategory;

      public readonly bool AllowsBoundParameters;
      public readonly bool HasMaterialQuantities;
      public readonly bool IsCuttable;
      public readonly bool IsVisibleInUI;

#if DEBUG
      static string CallerFilePath([System.Runtime.CompilerServices.CallerFilePath] string CallerFilePath = "") => CallerFilePath;
      static string SourceCodePath => Path.GetDirectoryName(CallerFilePath());

      internal static Definition ToDefinition(BuiltInCategory value, Document document)
      {
        var category = document.GetCategory(value);

        return new Definition
        (
          fullName: category?.Name ?? string.Empty,
          categoryType: category?.CategoryType ?? CategoryType.Invalid,
          isTagCategory: category?.IsTagCategory ?? false,
          parent: category.Parent?.Id.ToBuiltInCategory() ?? BuiltInCategory.INVALID,
          id: value,
          canAddSubcategory: category?.CanAddSubcategory ?? false,
          allowsBoundParameters: category?.AllowsBoundParameters ?? false,
          hasMaterialQuantities: category?.HasMaterialQuantities ?? false,
          isCuttable: category?.IsCuttable ?? false,
          isVisibleInUI: category?.IsVisibleInUI() ?? false
        );
      }

      public static void WriteDefinitions(Document document)
      {
        var definitions = BuiltInCategories.Select(bic => ToDefinition(bic, document)).
          OrderBy(x => x.CategoryType).
          ThenBy(x => x.IsTagCategory).
          ThenBy(x => (x.Parent.IsValid() ? x.Parent : x.Id).ToString()).
          ThenBy(x => x.Parent.IsValid()).
          ThenBy(x => x.Id.ToString()).
          ToList();

        var path = Path.Combine
        (
          SourceCodePath,
          "..",
          "Schemas",
          document.Application.VersionNumber,
          "BuiltInCategory.cs"
        );

        using (var writer = new System.CodeDom.Compiler.IndentedTextWriter(File.CreateText(path), "  "))
        {
          writer.WriteLine("using System.Collections.Generic;");
          writer.WriteLine("using System.Linq;");
          writer.WriteLine("using Autodesk.Revit.DB;");
          writer.WriteLine();
          writer.WriteLine("namespace RhinoInside.Revit.External.DB.Extensions");
          writer.WriteLine("{");
          writer.Indent++;

          writer.WriteLine("using BIC = BuiltInCategory;");
          writer.WriteLine("using CT = CategoryType;");
          writer.WriteLine();

          writer.WriteLine("public partial class BuiltInCategoryExtension");
          writer.WriteLine("{");
          writer.Indent++;
          writer.WriteLine("static readonly Definition[] _Definitions = new Definition[]");
          writer.WriteLine("{");
          writer.Indent++;

          foreach (var ct in Enum.GetValues(typeof(CategoryType)).Cast<CategoryType>().Skip(1))
          {
            writer.WriteLine($"#region {ct}");

            foreach (var d in definitions.Where(x => x.CategoryType == ct))
            {
              writer.Write($"new Definition(");
              if (d.Parent.IsValid()) writer.Write("  ");
              writer.Write($"{$"\"{d.Name}\", ",-64}");
              if (!d.Parent.IsValid()) writer.Write("  ");
              writer.Write($"{$"CT.{d.CategoryType}, ",-20}");
              writer.Write($"{$"{d.IsTagCategory.ToString().ToLower()}, ",-7}");
              writer.Write($"{$"/*{(int) d.Parent,9}*/ BIC.{d.Parent}, ",-50}");
              writer.Write($"{$"/*{(int) d.Id,9}*/ BIC.{d.Id}, ",-67}");
              writer.Write($"{$"{d.CanAddSubcategory.ToString().ToLower()}, ",-7}");
              writer.Write($"{$"{d.AllowsBoundParameters.ToString().ToLower()}, ",-7}");
              writer.Write($"{$"{d.HasMaterialQuantities.ToString().ToLower()}, ",-7}");
              writer.Write($"{$"{d.IsCuttable.ToString().ToLower()}, ",-7}");
              writer.Write($"{$"{d.IsVisibleInUI.ToString().ToLower()}",-7}");
              writer.WriteLine($"),");
            }

            writer.WriteLine($"#endregion");
            writer.WriteLine();
          }

          writer.Indent--;
          writer.WriteLine("};");

          writer.WriteLine();
          writer.WriteLine("private static readonly Dictionary<BIC, Definition> Definitions = _Definitions.ToDictionary(x => x.Id);");

          writer.Indent--;
          writer.WriteLine("}");

          writer.Indent--;
          writer.WriteLine("}");

          writer.Close();
        }
      }
#endif
    }
   }
}
