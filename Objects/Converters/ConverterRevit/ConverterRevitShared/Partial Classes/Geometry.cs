﻿using Autodesk.Revit.DB;
using Objects.Geometry;
using Objects.Primitive;
using System;
using System.Collections.Generic;
using System.Linq;
using Arc = Objects.Geometry.Arc;
using Curve = Objects.Geometry.Curve;
using DB = Autodesk.Revit.DB;
using Ellipse = Objects.Geometry.Ellipse;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Surface = Objects.Geometry.Surface;

namespace Objects.Converter.Revit
{
  /// <summary>
  ///Internal helper methods used for converison
  /// </summary>
  public partial class ConverterRevit
  {
    public object GeometryToNative(IHasBoundingBox geom)
    {
      switch (geom)
      {
        case Point g:
          return PointToNative(g);
        case ICurve g:
          return CurveToNative(g);
        case Plane g:
          return PlaneToNative(g);
        case Vector g:
          return VectorToNative(g);

        default:
          throw new Exception("Cannot convert Curve of type " + geom.GetType());
      }
    }
    public XYZ PointToNative(Point pt)
    {
      return new XYZ(ScaleToNative(pt.value[0], pt.units), ScaleToNative(pt.value[1], pt.units), ScaleToNative(pt.value[2], pt.units));
    }

    public Point PointToSpeckle(XYZ pt)
    {
      return new Point(ScaleToSpeckle(pt.X), ScaleToSpeckle(pt.Y), ScaleToSpeckle(pt.Z), ModelUnits);
    }

    public XYZ VectorToNative(Vector pt)
    {
      return new XYZ(ScaleToNative(pt.value[0], pt.units), ScaleToNative(pt.value[1], pt.units), ScaleToNative(pt.value[2], pt.units));
    }

    public DB.Plane PlaneToNative(Plane plane)
    {
      return DB.Plane.CreateByOriginAndBasis(PointToNative(plane.origin), VectorToNative(plane.xdir).Normalize(), VectorToNative(plane.ydir).Normalize());
    }

    public Plane PlaneToSpeckle(DB.Plane plane)
    {
      var origin = PointToSpeckle(plane.Origin);
      var normal = new Vector(ScaleToSpeckle(plane.Normal.X), ScaleToSpeckle(plane.Normal.Y), ScaleToSpeckle(plane.Normal.Z), ModelUnits);
      var xdir = new Vector(ScaleToSpeckle(plane.XVec.X), ScaleToSpeckle(plane.XVec.Y), ScaleToSpeckle(plane.XVec.Z), ModelUnits);
      var ydir = new Vector(ScaleToSpeckle(plane.YVec.X), ScaleToSpeckle(plane.YVec.Y), ScaleToSpeckle(plane.YVec.Z), ModelUnits);

      return new Plane(origin, normal, xdir, ydir, ModelUnits);
    }

    public DB.Line LineToNative(Line line)
    {
      return DB.Line.CreateBound(
        new XYZ(ScaleToNative(line.value[0], line.units), ScaleToNative(line.value[1], line.units), ScaleToNative(line.value[2], line.units)),
        new XYZ(ScaleToNative(line.value[3], line.units), ScaleToNative(line.value[4], line.units), ScaleToNative(line.value[5], line.units)));
    }

    public Line LineToSpeckle(DB.Line line)
    {
      var l = new Line() { value = new List<double>(), units = ModelUnits };
      l.value.AddRange(PointToSpeckle(line.GetEndPoint(0)).value);
      l.value.AddRange(PointToSpeckle(line.GetEndPoint(1)).value);
      return l;
    }

    public Circle CircleToSpeckle(DB.Arc arc)
    {
      // see https://forums.autodesk.com/t5/revit-api-forum/how-to-retrieve-startangle-and-endangle-of-arc-object/td-p/7637128
      var arcPlane = DB.Plane.CreateByNormalAndOrigin(arc.Normal, arc.Center);

      var c = new Circle(PlaneToSpeckle(arcPlane), ScaleToSpeckle(arc.Radius), ModelUnits);
      return c;
    }

    public DB.Arc CircleToNative(Circle circle)
    {
      var plane = PlaneToNative(circle.plane);
      return DB.Arc.Create(plane, ScaleToNative((double)circle.radius, circle.units), 0, 2 * Math.PI);
    }

    public DB.Arc ArcToNative(Arc arc)
    {
      double startAngle, endAngle;
      if (arc.startAngle > arc.endAngle)
      {
        startAngle = (double)arc.endAngle;
        endAngle = (double)arc.startAngle;
      }
      else
      {
        startAngle = (double)arc.startAngle;
        endAngle = (double)arc.endAngle;
      }
      var plane = PlaneToNative(arc.plane);
      return DB.Arc.Create(PointToNative(arc.startPoint), PointToNative(arc.endPoint), PointToNative(arc.midPoint));
      //return Arc.Create( plane.Origin, (double) arc.Radius * Scale, startAngle, endAngle, plane.XVec, plane.YVec );
    }



    public Arc ArcToSpeckle(DB.Arc arc)
    {

      // see https://forums.autodesk.com/t5/revit-api-forum/how-to-retrieve-startangle-and-endangle-of-arc-object/td-p/7637128
      var arcPlane = DB.Plane.CreateByNormalAndOrigin(arc.Normal, arc.Center);

      XYZ center = arc.Center;


      XYZ dir0 = (arc.GetEndPoint(0) - center).Normalize();
      XYZ dir1 = (arc.GetEndPoint(1) - center).Normalize();

      XYZ start = arc.Evaluate(0, true);
      XYZ end = arc.Evaluate(1, true);
      XYZ mid = arc.Evaluate(0.5, true);

      double startAngle = dir0.AngleOnPlaneTo(arc.XDirection, arc.Normal);
      double endAngle = dir1.AngleOnPlaneTo(arc.XDirection, arc.Normal);

      var a = new Arc(PlaneToSpeckle(arcPlane), ScaleToSpeckle(arc.Radius), startAngle, endAngle, endAngle - startAngle, ModelUnits);
      a.endPoint = PointToSpeckle(end);
      a.startPoint = PointToSpeckle(start);
      a.midPoint = PointToSpeckle(mid);
      return a;
    }

    public DB.Ellipse EllipseToNative(Ellipse ellipse)
    {
      //TODO: support ellipse arcs
      using (DB.Plane basePlane = PlaneToNative(ellipse.plane))
      {
        var e = DB.Ellipse.CreateCurve(
          PointToNative(ellipse.plane.origin),
          ScaleToNative((double)ellipse.firstRadius, ellipse.units),
          ScaleToNative((double)ellipse.secondRadius, ellipse.units),
          basePlane.XVec.Normalize(),
          basePlane.YVec.Normalize(),
          0,
           2 * Math.PI
          ) as DB.Ellipse;

        e.MakeBound(ellipse.trimDomain?.start ?? 0, ellipse.trimDomain?.end ?? 2 * Math.PI);
        return e;
      }
    }

    public Ellipse EllipseToSpeckle(DB.Ellipse ellipse)
    {
      using (DB.Plane basePlane = DB.Plane.CreateByOriginAndBasis(ellipse.Center, ellipse.XDirection, ellipse.YDirection))
      {
        var trim = ellipse.IsBound ? new Interval(ellipse.GetEndParameter(0), ellipse.GetEndParameter(1)) : null;

        return new Ellipse(
          PlaneToSpeckle(basePlane),
          ScaleToSpeckle(ellipse.RadiusX),
          ScaleToSpeckle(ellipse.RadiusY),
          new Interval(0, 2 * Math.PI),
          trim,
          ModelUnits);
      }
    }

    public Curve NurbsToSpeckle(DB.NurbSpline revitCurve)
    {
      var points = new List<double>();
      foreach (var p in revitCurve.CtrlPoints)
      {
        points.AddRange(PointToSpeckle(p).value);
      }

      Curve speckleCurve = new Curve();
      speckleCurve.weights = revitCurve.Weights.Cast<double>().ToList();
      speckleCurve.points = points;
      speckleCurve.knots = revitCurve.Knots.Cast<double>().ToList(); ;
      speckleCurve.degree = revitCurve.Degree;
      //speckleCurve.periodic = revitCurve.Period;
      speckleCurve.rational = revitCurve.isRational;
      speckleCurve.closed = RevitVersionHelper.IsCurveClosed(revitCurve);
      speckleCurve.units = ModelUnits;
      //speckleCurve.domain = new Interval(revitCurve.StartParameter(), revitCurve.EndParameter());

      return speckleCurve;
    }


    public DB.Curve CurveToNative(Curve speckleCurve)
    {
      var pts = new List<XYZ>();
      for (int i = 0; i < speckleCurve.points.Count; i += 3)
      {
        pts.Add(new XYZ(
          ScaleToNative(speckleCurve.points[i], speckleCurve.units),
          ScaleToNative(speckleCurve.points[i + 1], speckleCurve.units),
          ScaleToNative(speckleCurve.points[i + 2], speckleCurve.units)));
      }
      try
      {
        if (speckleCurve.knots != null && speckleCurve.weights != null && speckleCurve.knots.Any() && speckleCurve.weights.Any())
        {
          var weights = speckleCurve.weights.GetRange(0, pts.Count);
          var speckleKnots = new List<double>(speckleCurve.knots);
          if (speckleKnots.Count != pts.Count + speckleCurve.degree + 1)
          {
            // Curve has rhino knots, repeat first and last.
            speckleKnots.Insert(0, speckleKnots[0]);
            speckleKnots.Add(speckleKnots[speckleKnots.Count - 1]);
          }
          //var knots = speckleKnots.GetRange(0, pts.Count + speckleCurve.degree + 1);
          var curve = NurbSpline.CreateCurve(speckleCurve.degree, speckleKnots, pts, weights);
          return curve;
        }
        else
        {
          var weights = speckleCurve.weights.GetRange(0, pts.Count);
          var curve = NurbSpline.CreateCurve(pts, weights);
          return curve;
        }

      }
      catch (Exception)
      {
        return null;
      }
    }

    /// <summary>
    /// Recursively creates an ordered list of curves from a polycurve/polyline.
    /// Please note that a polyline is broken down into lines.
    /// </summary>
    /// <param name="crv">A speckle curve.</param>
    /// <returns></returns>
    public CurveArray CurveToNative(ICurve crv)
    {
      CurveArray curveArray = new CurveArray();
      switch (crv)
      {
        case Line line:
          curveArray.Append(LineToNative(line));
          return curveArray;

        case Arc arc:
          curveArray.Append(ArcToNative(arc));
          return curveArray;

        case Circle circle:
          curveArray.Append(CircleToNative(circle));
          return curveArray;

        case Ellipse ellipse:
          curveArray.Append(EllipseToNative(ellipse));
          return curveArray;

        case Curve nurbs:
          curveArray.Append(CurveToNative(nurbs));
          return curveArray;

        case Polyline poly:
          return PolylineToNative(poly);

        case Polycurve plc:
          foreach (var seg in plc.segments)
          {
            curveArray.Append(CurveToNative(seg).get_Item(0));
          }

          return curveArray;
        default:
          throw new Exception("The provided geometry is not a valid curve");
      }
    }

    public ICurve CurveToSpeckle(DB.Curve curve)
    {
      switch (curve)
      {
        case DB.Line line:
          return LineToSpeckle(line);
        case DB.Arc arc:
          if (!arc.IsBound)
          {
            return (CircleToSpeckle(arc));
          }

          return ArcToSpeckle(arc);
        case DB.Ellipse ellipse:
          return EllipseToSpeckle(ellipse);
        case DB.NurbSpline nurbs:
          return NurbsToSpeckle(nurbs);
        default:
          throw new Exception("Cannot convert Curve of type " + curve.GetType());
      }
    }

    public CurveArray PolylineToNative(Polyline polyline)
    {
      var curveArray = new CurveArray();
      if (polyline.value.Count == 6)
      {
        curveArray.Append(LineToNative(new Line(polyline.value, ModelUnits)));
      }
      else
      {
        List<Point> pts = polyline.points;

        for (int i = 1; i < pts.Count; i++)
        {
          var speckleLine = new Line(new double[] { pts[i - 1].value[0], pts[i - 1].value[1], pts[i - 1].value[2], pts[i].value[0], pts[i].value[1], pts[i].value[2] }, ModelUnits);

          curveArray.Append(LineToNative(speckleLine));
        }

        if (polyline.closed)
        {
          var speckleLine = new Line(new double[] { pts[pts.Count - 1].value[0], pts[pts.Count - 1].value[1], pts[pts.Count - 1].value[2], pts[0].value[0], pts[0].value[1], pts[0].value[2] }, ModelUnits);
          curveArray.Append(LineToNative(speckleLine));
        }
      }
      return curveArray;
    }

    // Insipred by
    // https://github.com/DynamoDS/DynamoRevit/blob/master/src/Libraries/RevitNodes/GeometryConversion/ProtoToRevitMesh.cs
    public IList<GeometryObject> MeshToNative(Mesh mesh)
    {

      TessellatedShapeBuilderTarget target = TessellatedShapeBuilderTarget.Mesh;
      TessellatedShapeBuilderFallback fallback = TessellatedShapeBuilderFallback.Salvage;

      var tsb = new TessellatedShapeBuilder() { Fallback = fallback, Target = target, GraphicsStyleId = ElementId.InvalidElementId };
      tsb.OpenConnectedFaceSet(false);

      var vertices = ArrayToPoints(mesh.vertices, mesh.units);

      int i = 0;

      while (i < mesh.faces.Count)
      {
        var points = new List<XYZ>();

        if (mesh.faces[i] == 0)
        { // triangle
          points = new List<XYZ> { vertices[mesh.faces[i + 1]], vertices[mesh.faces[i + 2]], vertices[mesh.faces[i + 3]] };
          i += 4;
        }
        else
        { // quad
          points = new List<XYZ> { vertices[mesh.faces[i + 1]], vertices[mesh.faces[i + 2]], vertices[mesh.faces[i + 3]], vertices[mesh.faces[i + 4]] };
          i += 5;
        }

        var face = new TessellatedFace(points, ElementId.InvalidElementId);
        tsb.AddFace(face);
      }

      tsb.CloseConnectedFaceSet();
      tsb.Build();
      var result = tsb.GetBuildResult();
      return result.GetGeometricalObjects();


    }

    public XYZ[] ArrayToPoints(IEnumerable<double> arr, string units)
    {
      if (arr.Count() % 3 != 0)
      {
        throw new Exception("Array malformed: length%3 != 0.");
      }

      XYZ[] points = new XYZ[arr.Count() / 3];
      var asArray = arr.ToArray();
      for (int i = 2, k = 0; i < arr.Count(); i += 3)
      {
        points[k++] = new XYZ(ScaleToNative(asArray[i - 2], units), ScaleToNative(asArray[i - 1], units), ScaleToNative(asArray[i], units));
      }

      return points;
    }

    //https://github.com/DynamoDS/DynamoRevit/blob/f8206726d8a3aa5bf06f5dbf7ce8a732bb025c34/src/Libraries/RevitNodes/GeometryConversion/GeometryPrimitiveConverter.cs#L201
    public XYZ GetPerpendicular(XYZ xyz)
    {
      var ixn = xyz.Normalize();
      var xn = new XYZ(1, 0, 0);

      if (ixn.IsAlmostEqualTo(xn))
      {
        xn = new XYZ(0, 1, 0);
      }

      return ixn.CrossProduct(xn).Normalize();
    }

    public Geometry.Surface FaceToSpeckle(DB.Face face, DB.BoundingBoxUV uvBox)
    {

#if Revit2021
      var surf = DB.ExportUtils.GetNurbsSurfaceDataForSurface(face.GetSurface());
#else
      var surf = DB.ExportUtils.GetNurbsSurfaceDataForFace(face);
#endif

      var spcklSurface = NurbsSurfaceToSpeckle(surf, face.GetBoundingBox());
      return spcklSurface;
    }

    public Geometry.Surface NurbsSurfaceToSpeckle(DB.NurbsSurfaceData surface, DB.BoundingBoxUV uvBox)
    {
      var result = new Geometry.Surface();

      result.units = ModelUnits;

      result.degreeU = surface.DegreeU;
      result.degreeV = surface.DegreeV;

      var knotsU = surface.GetKnotsU().ToList();
      var knotsV = surface.GetKnotsV().ToList();

      result.knotsU = knotsU.GetRange(1, knotsU.Count - 2);
      result.knotsV = knotsV.GetRange(1, knotsV.Count - 2);

      var controlPointCountU = result.knotsU.Count - result.degreeU - 1;
      var controlPointCountV = result.knotsV.Count - result.degreeV - 1;

      var controlPoints = surface.GetControlPoints();
      var weights = surface.GetWeights();

      var points = new List<List<ControlPoint>>();
      for (var u = 0; u < controlPointCountU; u++)
      {
        var uOffset = u * controlPointCountV;
        var row = new List<ControlPoint>();

        for (var v = 0; v < controlPointCountV; v++)
        {
          var pt = controlPoints[uOffset + v];
          if (surface.IsRational)
          {
            var w = weights[uOffset + v];
            row.Add(new ControlPoint(pt.X, pt.Y, pt.Z, w, ModelUnits));
          }
          else
          {
            row.Add(new ControlPoint(pt.X, pt.Y, pt.Z, ModelUnits));
          }
        }
      }

      return result;
    }


    public BRepBuilderEdgeGeometry BrepEdgeToNative(BrepEdge edge)
    {
      var edgeCurve = edge.Curve as Curve;

      // TODO: Trim curve with domain. Unsure if this is necessary as all our curves are converted to NURBS on Rhino output.

      var nativeCurve = CurveToNative(edgeCurve);
      if (edge.ProxyCurveIsReversed)
      {
        nativeCurve = nativeCurve.CreateReversed();
      }

      // TODO: Remove short segments if smaller than 'Revit.ShortCurveTolerance'.
      var edgeGeom = BRepBuilderEdgeGeometry.Create(nativeCurve);
      return edgeGeom;
    }

    public double[] ControlPointWeightsToNative(List<List<ControlPoint>> controlPoints)
    {
      var uCount = controlPoints.Count;
      var vCount = controlPoints[0].Count;
      var count = uCount * vCount;
      var weights = new double[count];
      int p = 0;

      controlPoints.ForEach(row =>
        row.ForEach(pt =>
          weights[p++] = pt.weight));

      return weights;
    }

    public DB.XYZ[] ControlPointsToNative(List<List<ControlPoint>> controlPoints)
    {
      var uCount = controlPoints.Count;
      var vCount = controlPoints[0].Count;
      var count = uCount * vCount;
      var points = new DB.XYZ[count];
      int p = 0;

      controlPoints.ForEach(row =>
        row.ForEach(pt =>
          points[p++] = new DB.XYZ(ScaleToNative(pt.x, pt.units), ScaleToNative(pt.y, pt.units), ScaleToNative(pt.z, pt.units))));

      return points;
    }

    public double[] SurfaceKnotsToNative(List<double> list)
    {
      var count = list.Count;
      var knots = new double[count + 2];

      int j = 0, k = 0;
      while (j < count)
      {
        knots[++k] = list[j++];
      }

      knots[0] = knots[1];
      knots[count + 1] = knots[count];

      return knots;
    }

    public BRepBuilderSurfaceGeometry BrepFaceToNative(BrepFace face)
    {
      var surface = face.Surface;
      var uvBox = new DB.BoundingBoxUV(surface.knotsU[0], surface.knotsV[0], surface.knotsU[surface.knotsU.Count - 1], surface.knotsV[surface.knotsV.Count - 1]);
      var surfPts = surface.GetControlPoints();
      var uKnots = SurfaceKnotsToNative(surface.knotsU);
      var vKnots = SurfaceKnotsToNative(surface.knotsV);
      var cPts = ControlPointsToNative(surfPts);

      BRepBuilderSurfaceGeometry result;
      if (!surface.rational)
      {
        result = DB.BRepBuilderSurfaceGeometry.CreateNURBSSurface(surface.degreeU, surface.degreeV, uKnots,
          vKnots, cPts, false, uvBox);
      }
      else
      {
        var weights = ControlPointWeightsToNative(surfPts);
        result = DB.BRepBuilderSurfaceGeometry.CreateNURBSSurface(surface.degreeU, surface.degreeV, uKnots,
          vKnots, cPts, weights, false, uvBox);
      }
      return result;
    }


    public Solid BrepToNative(Brep brep)
    {
      switch (brep.Orientation)
      {
        case BrepOrientation.Inward:
          break;
        case BrepOrientation.Outward:
          break;
      }

      using var builder = new BRepBuilder(brep.IsClosed ? BRepType.Solid : BRepType.OpenShell);
      builder.SetAllowShortEdges();
      builder.AllowRemovalOfProblematicFaces();

      var brepEdges = new List<DB.BRepBuilderGeometryId>[brep.Edges.Count];
      foreach (var face in brep.Faces)
      {
        var faceId = builder.AddFace(BrepFaceToNative(face), face.OrientationReversed);

        foreach (var loop in face.Loops)
        {
          var loopId = builder.AddLoop(faceId);
          if (face.OrientationReversed)
          {
            loop.TrimIndices.Reverse();
          }

          foreach (var trim in loop.Trims)
          {
            if (trim.TrimType != BrepTrimType.Boundary && trim.TrimType != BrepTrimType.Mated)
            {
              continue;
            }

            if (trim.Edge == null)
            {
              continue;
            }

            var edgeIds = brepEdges[trim.EdgeIndex];
            if (edgeIds == null)
            {
              // First time we see this edge, convert it and add
              edgeIds = brepEdges[trim.EdgeIndex] = new List<BRepBuilderGeometryId>();
              edgeIds.Add(builder.AddEdge(BrepEdgeToNative(trim.Edge)));
            }

            var trimReversed = face.OrientationReversed ? !trim.IsReversed : trim.IsReversed;
            if (trimReversed)
            {
              for (int e = edgeIds.Count - 1; e >= 0; --e)
              {
                builder.AddCoEdge(loopId, edgeIds[e], true);
              }
            }
            else
            {
              for (int e = 0; e < edgeIds.Count; ++e)
              {
                builder.AddCoEdge(loopId, edgeIds[e], false);
              }
            }
          }

          builder.FinishLoop(loopId);
        }
        builder.FinishFace(faceId);
      }

      var bRepBuilderOutcome = builder.Finish();
      if (bRepBuilderOutcome == BRepBuilderOutcome.Failure)
      {
        return null;
      }

      var isResultAvailable = builder.IsResultAvailable();
      if (!isResultAvailable)
      {
        return null;
      }

      var result = builder.GetResult();
      return result;
    }

    public Brep BrepToSpeckle(Solid solid)
    {
      // TODO: Incomplete implementation!!

      var brep = new Brep();
      brep.units = ModelUnits;

      if (solid is null || solid.Faces.IsEmpty)
      {
        return null;
      }

      var brepEdges = new Dictionary<DB.Edge, BrepEdge>();

      foreach (var face in solid.Faces.Cast<DB.Face>())
      {
        var si = AddSurface(brep, face, out var shells, brepEdges);
        if (si < 0)
        {
          continue;
        }

        TrimSurface(brep, si, !face.OrientationMatchesSurfaceOrientation, shells);
      }

      // TODO: Revit has no brep vertices. Must call 'brep.SetVertices()' in rhino when provenance is revit.
      // TODO: Set tolerances and flags in rhino when provenance is revit.

      return brep;
    }

    public Surface FaceToSpeckle(DB.Face face, out bool parametricOrientation, double relativeTolerance = 0.0)
    {
      using (var surface = face.GetSurface())
      {
        parametricOrientation = surface.OrientationMatchesParametricOrientation;
      }

      switch (face)
      {
        case null: return null;
        //case PlanarFace planar:            return ToRhinoSurface(planar, relativeTolerance);
        //case ConicalFace conical:          return ToRhinoSurface(conical, relativeTolerance);
        //case CylindricalFace cylindrical:  return ToRhinoSurface(cylindrical, relativeTolerance);
        //case RevolvedFace revolved:        return ToRhinoSurface(revolved, relativeTolerance);
        //case RuledFace ruled:              return ToRhinoSurface(ruled, relativeTolerance);
        case HermiteFace hermite: return FaceToSpeckle(hermite, face.GetBoundingBox());
        default: throw new NotImplementedException();
      }
    }

    public int AddSurface(Brep brep, DB.Face face, out List<BrepBoundary>[] shells,
      Dictionary<DB.Edge, BrepEdge> brepEdges = null)
    {
      throw new NotImplementedException();
    }

    public void TrimSurface(Brep brep, int surface, bool orientationReversed, List<BrepBoundary>[] shells)
    {
      // TODO: Incomplete method.
      foreach (var shell in shells)
      {
        //var sFace = new BrepFace(brep,surface,null,null,orientationReversed);

        foreach (var loop in shell)
        {
          var edgeCount = loop.edges.Count;

          for (int e = 0; e < edgeCount; ++e)
          {
            var brepEdge = loop.edges[e];
            var orientation = loop.orientation[e];
            if (orientation == 0)
            {
              continue;
            }

            if (loop.trims.segments[e] is Curve trim)
            {
              brep.Curve2D.Add(trim);
              // TODO: Missing stuff here!
            }
          }
        }

      }
      throw new NotImplementedException();
    }

    public struct BrepBoundary
    {
      public BrepLoopType type;
      public List<BrepEdge> edges;
      public Polycurve trims;
      public List<int> orientation;
    }
  }
}
