cd %1\NORCE.Drilling.Field.Model
(docfx build -o %1\NORCE.Drilling.Field.Service\wwwroot\Field\ModelAPI) || (echo "docfx for model failed")
cd %1\NORCE.Drilling.Field.Service
(docfx build -o %1\NORCE.Drilling.Field.Service\wwwroot\Field\ServiceAPI) || (echo "docfx for service failed")
