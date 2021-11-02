using Elements;
using Elements.Geometry;
using Elements.Spatial;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CrossFunctionOverrideFacadeExample
{
    public static class CrossFunctionOverrideFacadeExample
    {
        /// <summary>
        /// The CrossFunctionOverrideFacadeExample function.
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A CrossFunctionOverrideFacadeExampleOutputs instance containing computed results and the model with any new elements.</returns>
        public static CrossFunctionOverrideFacadeExampleOutputs Execute(Dictionary<string, Model> inputModels, CrossFunctionOverrideFacadeExampleInputs input)
        {
            var output = new CrossFunctionOverrideFacadeExampleOutputs();
            var envelopes = inputModels["Envelope"].AllElementsOfType<Envelope>();
            foreach (var envelope in envelopes)
            {
                // create proxy
                var envelopeProxy = envelope.Proxy(FacadeGridSettingsOverride.Name);

                // the FacadeGridSettingsValue type is already
                // generated for us — it is useful to use it here,
                // even though we're creating it ourselves, to start.
                var gridSettings = new FacadeGridSettingsValue(3, 3);
                if (input.Overrides?.FacadeGridSettings != null)
                {
                    // find a matching override by comparing the override's identity with the envelope's properties
                    var match = input.Overrides.FacadeGridSettings.FirstOrDefault(o => IdentityMatch(envelope, o));
                    // if we found a match, use it to set different grid settings.
                    if (match != null)
                    {
                        gridSettings = match.Value;
                        // attach the override's identity to the proxy
                        Identity.AddOverrideIdentity(envelopeProxy, match);
                    }
                }

                // attach grid settings values to proxy element
                Identity.AddOverrideValue(envelopeProxy, FacadeGridSettingsOverride.Name, gridSettings);
                // add proxy to output model
                output.Model.AddElement(envelopeProxy);

                // create grid elements for an envelope and grid settings
                var grid = CreateGridForEnvelope(envelope, gridSettings);
                output.Model.AddElements(grid);
            }
            return output;
        }

        private static bool IdentityMatch(Envelope envelope, FacadeGridSettingsOverride o)
        {
            return o.Identity.Profile.Perimeter.Centroid().DistanceTo(envelope.Profile.Perimeter.Centroid()) < 0.01 && Math.Abs(envelope.Elevation - o.Identity.Elevation) < 0.01;
        }

        private static List<ModelCurve> CreateGridForEnvelope(Envelope envelope, FacadeGridSettingsValue gridSettings)
        {
            List<ModelCurve> grid = new List<ModelCurve>();
            foreach (var solidOp in envelope.Representation.SolidOperations)
            {
                var faces = solidOp.Solid.Faces;
                foreach (var face in faces)
                {
                    var faceBoundary = face.Value.Outer.ToPolygon().TransformedPolygon(envelope.Transform);
                    // skip non-vertical faces
                    if (Math.Abs(faceBoundary.Normal().Dot(Vector3.ZAxis)) > 0.01)
                    {
                        continue;
                    }
                    var grid2d = new Grid2d(faceBoundary);
                    grid2d.U.DivideByApproximateLength(gridSettings.XGridSize);
                    grid2d.V.DivideByApproximateLength(gridSettings.YGridSize);
                    grid.AddRange(grid2d.ToModelCurves());
                }
            }
            return grid;
        }
    }
}