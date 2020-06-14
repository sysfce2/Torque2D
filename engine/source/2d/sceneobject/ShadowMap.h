#ifndef _SHADOWMAP_H_
#define _SHADOWMAP_H_

#ifndef _SCENE_OBJECT_H_
#include "2d/sceneobject/SceneObject.h"
#endif

class ShadowMap : public SceneObject

{
   typedef SceneObject Parent;

protected:
   F32            mLightRadius;

public:
   ShadowMap();
   ~ShadowMap();

   static void initPersistFields();

   inline void setLightRadius(const F32 lightRadius) { mLightRadius = lightRadius; }
   inline F32 getLightRadius(void) const { return mLightRadius; }

   virtual bool onAdd();
   virtual void onRemove();

   virtual void safeDelete(void);
   virtual void sceneRender(const SceneRenderState* sceneRenderState, const SceneRenderRequest* sceneRenderRequest, BatchRender* batchRender);
   //virtual bool validRender(void) const {}
   virtual bool shouldRender(void) const { return true; }

   void processObject(SceneObject *obj);
   void renderShadow(const Vector<Vector2>& verts, const Vector2& lightPos);

   DECLARE_CONOBJECT(ShadowMap);


   protected:
      virtual void OnRegisterScene(Scene* mScene);
      virtual void OnUnregisterScene(Scene* mScene);

private:

};


#endif //_SHADOWMAP_H_
