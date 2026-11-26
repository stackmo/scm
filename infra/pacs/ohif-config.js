window.config = {
  routerBasename: '/pacs/ohif',
  extensions: [],
  modes: [],
  showStudyList: false,
  maxNumberOfWebWorkers: 4,
  showWarningMessageForCrossOrigin: false,
  showCPUFallbackMessage: true,
  strictZSpacingForVolumeViewport: true,
  defaultDataSourceName: 'dicomweb',
  dataSources: [
    {
      namespace: '@ohif/extension-default.dataSourcesModule.dicomweb',
      sourceName: 'dicomweb',
      friendlyName: 'SCM Orthanc PACS DICOMweb',
      configuration: {
        name: 'orthanc',
        wadoUriRoot: '/api/pacs-proxy/wado',
        qidoRoot: '/api/pacs-proxy/dicom-web',
        wadoRoot: '/api/pacs-proxy/dicom-web',
        qidoSupportsIncludeField: false,
        supportsReject: false,
        imageRendering: 'wadors',
        thumbnailRendering: 'wadors',
        enableStudyLazyLoad: true,
        supportsFuzzyMatching: false,
        supportsWildcard: true,
        omitQuotationForMultipartRequest: true
      }
    }
  ]
};
